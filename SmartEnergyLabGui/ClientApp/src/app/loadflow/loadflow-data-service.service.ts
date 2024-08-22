import { EventEmitter, Injectable } from '@angular/core';
import { Branch, Ctrl, Dataset, DatasetData, DatasetType, GISData, GridSubstation, GridSubstationLocation, ILoadflowLink, ILoadflowLocation, LoadflowCtrlType, LoadflowResults, LocationData, NetworkData, Node, UpdateLocationData } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';
import { SignalRService } from '../main/signal-r-status/signal-r.service';
import { ShowMessageService } from '../main/show-message/show-message.service';
import { DialogService } from '../dialogs/dialog.service';
import { MessageDialog } from '../dialogs/message-dialog/message-dialog.component';
import { DatasetsService } from '../datasets/datasets.service';

type NodeDict = {
    [code: string]:Node
}

export type SelectedMapItem = {location: ILoadflowLocation | null, link: ILoadflowLink | null}

@Injectable({
    providedIn: 'root'
})

export class LoadflowDataService {

    constructor(private dataClientService: DataClientService, 
        private signalRService: SignalRService, 
        private messageService: ShowMessageService,
        private dialogService: DialogService) { 
        this.gridSubstations = [];
        this.networkData = { 
            nodes: { tableName: '',data:[], userEdits: [], deletedData: [] }, 
            branches: { tableName: '',data:[], userEdits: [], deletedData: [] }, 
            ctrls: { tableName: '',data:[], userEdits: [],deletedData: [] },
            boundaries: { tableName: '',data:[], userEdits: [],deletedData: [] },
            zones: { tableName: '',data:[], userEdits: [],deletedData: [] },
            locations: { tableName: '',data:[], userEdits: [],deletedData: [] },
        }
        this.locationData = { locations: [], links: []}
        this.locMap = new Map<number,LoadflowLocation>()
        this.linkMap = new Map<string,LoadflowLink>()
        this.signalRService.hubConnection.on('Loadflow_AllTripsProgress', (data) => {
            this.AllTripsProgress.emit(data);
        })
        //
        this.selectedMapItem = null
    }

    dataset: Dataset = {id: 0, type: DatasetType.Loadflow, name: '', parent: null, isReadOnly: true}
    gridSubstations: GridSubstation[]
    networkData: NetworkData
    locationData: LocationData
    locMap: Map<number,LoadflowLocation>
    linkMap: Map<string,LoadflowLink>
    loadFlowResults: LoadflowResults | undefined
    boundaryName: string | undefined
    inRun: boolean = false

    selectedMapItem: SelectedMapItem | null

    setDataset(dataset: Dataset) {
        this.dataset = dataset;
        //
        this.loadDataset();
    }

    private loadDataset() {
        this.loadNetworkData(false);
    }

    private loadNetworkData(withMessage: boolean) {
        if ( withMessage ) {
            this.messageService.showMessage('Loading ...')
        }
        this.dataClientService.GetNetworkData( this.dataset.id, (results)=>{
            this.networkData = results
            //??this.locationData = this.getLocationData(this.networkData)
            this.messageService.clearMessage()
            this.NetworkDataLoaded.emit(results);
            this.updateLocationData(true)
            //??this.LocationDataLoaded.emit(this.locationData);
            //?? not needed as 
            // re-select if location is still selected
            //if ( this.selectedMapItem?.location ) {
            //    let oldLoc = this.selectedMapItem.location
            //    this.selectLocation(oldLoc.id)
            //}
            // and link
            //if ( this.selectedMapItem?.link ) {
            //    let oldLink = this.selectedMapItem.link
            //    this.selectLink(oldLink.id)
            //}
        })
    }

    private old_getLocationData(nd: NetworkData):LocationData {
        //
        let locs = nd.locations.data
        let ctrls = nd.ctrls.data
        let branches = nd.branches.data
        //
        this.locMap = new Map<number,LoadflowLocation>()
        //
        for( let loc of locs) {
            let isQB = ctrls.find(m=>m.node1.location?.id == loc.id && m.type == LoadflowCtrlType.QB)!==undefined
            this.locMap.set(loc.id,new LoadflowLocation(loc));
        }  
        let locations = Array.from(this.locMap.values());

        // Links - include branches that connect different locations
        let visibleBranches = branches.filter( m=>this.isBranchExternal(m) );
        this.linkMap = new Map<string,LoadflowLink>();
        for( let b of visibleBranches) {
            let link:LoadflowLink | undefined;
            let keys = this.getBranchKeys(b);
            if ( !this.linkMap.has(keys.key1) && !this.linkMap.has(keys.key2)) {
                let ctrl = ctrls.find( m=>m.branchId === b.id);
                let isHVDC = ctrl?.type === LoadflowCtrlType.HVDC;
                link = new LoadflowLink(b);
                this.linkMap.set(keys.key1,link);
            }
        }
        let links = Array.from(this.linkMap.values());

        return { locations: locations, links: links}
              
    }

    private getBranchKeys(b: Branch): { key1: string, key2: string} {
        return {
            key1: `${b.node1LocationId}:${b.node2LocationId}`,
            key2: `${b.node2LocationId}:${b.node1LocationId}`
        }
    }

    setBound(boundaryName: string) {
        this.inRun = true;
        this.dataClientService.SetBound(this.dataset.id, boundaryName, (results) =>{
            this.inRun = false;
            this.boundaryName = boundaryName
            this.loadFlowResults = results
            this.ResultsLoaded.emit(results)
        })
    }

    runBaseLoadflow() {
        this.inRun = true;
        this.dataClientService.RunBaseLoadflow( this.dataset.id, (results) => {
            this.inRun = false;
            this.loadFlowResults = results;
            this.ResultsLoaded.emit(results);
        });
    }

    runBoundaryTrip(boundaryName:string, tripName: string) {
        this.inRun = true;
        this.dataClientService.RunBoundaryTrip( this.dataset.id, boundaryName, tripName, (results) => {
            this.inRun = false;
            this.loadFlowResults = results;
            this.ResultsLoaded.emit(results);
        });
    }

    runAllBoundaryTrips(boundaryName:string) {
        this.inRun = true;
        this.dataClientService.RunAllBoundaryTrips( this.dataset.id, boundaryName, (results) => {
            this.inRun = false;
            this.loadFlowResults = results;
            this.ResultsLoaded.emit(results);
        });
    }

    selectLocation(locId: number) {
        let loc = this.locationData.locations.find(m=>m.id==locId)
        if ( loc ) {
            this.selectedMapItem = { location: loc, link: null }
            this.ObjectSelected.emit(this.selectedMapItem)    
        }
    }

    getLocation(locId: number):ILoadflowLocation | undefined {
        let loc = this.locationData.locations.find(m=>m.id==locId)
        return loc
    }

    selectLink(branchId: number) {
        let branch = this.locationData.links.find(m=>m.id==branchId)
        if ( branch) {
            this.selectedMapItem = { location: null, link: branch }
            this.ObjectSelected.emit(this.selectedMapItem)    
        }
    }

    clearMapSelection() {
        this.selectedMapItem = { location: null, link: null} 
        this.ObjectSelected.emit(this.selectedMapItem)
    }

    reload() {
        this.loadDataset();
        if ( this.boundaryName!=null ) {
            this.setBound(this.boundaryName);
        }
    }

    searchLocations(str: string, maxResults: number):ILoadflowLocation[]  {
        let lowerStr = str.toLocaleLowerCase();
        var searchResults = this.locationData.locations.filter(m=>m.name.toLocaleLowerCase().includes(lowerStr)).slice(0,maxResults)
        return searchResults;
    }

    getBranchesWithoutCtrls() {
        // same location and voltage at either side of branch
        //??let ctrlBranches = this.networkData.branches.data.filter( m=>m.node1Code.substring(0,5) == m.node2Code.substring(0,5))
        let ctrlBranches = this.networkData.branches.data
        // now only include ones without ctrls
        let results:Branch[] = []
        for( let b of ctrlBranches) {
            let ctrl = this.networkData.ctrls.data.find( m=>m.branchId == b.id)
            if ( !ctrl ) {
                results.push(b)
            }
        }
        return results;
    }

    canDeleteNode(node: Node):boolean {
        let id = node.id
        let bs = this.networkData.branches.data.filter(m=>m.node1Id == id || m.node2Id== id)
        if ( bs.length>0 ) {
            this.dialogService.showMessageDialog(new MessageDialog(`Cannot delete node since it used by <b>${bs.length}</b> branches`))
            return false
        } else {
            return true
        }
    }

    canDeleteLocation(loc: GridSubstationLocation):boolean {
        let id = loc.id
        let bs = this.networkData.nodes.data.filter(m=>m.location?.id == loc.id)
        if ( bs.length>0 ) {
            this.dialogService.showMessageDialog(new MessageDialog(`Cannot delete location since it used by <b>${bs.length}</b> nodes`))
            return false
        } else {
            return true
        }
    }

    afterEdit(resp: DatasetData<any>[] ) {
        console.log('afterEdit')
        console.log(resp)
        //
        for( let r of resp) {
            let dd = this.getDatasetData(r.tableName)
            DatasetsService.updateDatasetData(dd,r)    
        }
        //
        this.NetworkDataLoaded.emit(this.networkData)
        //
        this.updateLocationData(false);
    }

    old_updateLocationData( resp: DatasetData<any>[]) {
        let updateLocsMap:Map<number,ILoadflowLocation> = new Map<number,ILoadflowLocation>()
        let updateLinksMap:Map<number,ILoadflowLink> = new Map<number,ILoadflowLink>()
        console.log('updateLocData')
        console.log(resp)
        for( let r of resp) {
            if ( r.tableName == "GridSubstationLocation") {
                for ( let d of r.data) {
                    let gsl:GridSubstationLocation = d
                    let loc = this.locMap.get(gsl.id)
                    if ( loc ) {
                        loc.setLocation(gsl)
                    } else {
                        loc = new LoadflowLocation(gsl)
                        this.locMap.set(loc.id,loc)
                    }
                    if ( !updateLocsMap.has(loc.id)) {
                        updateLocsMap.set(loc.id,loc);
                    }
                }
            }
            if ( r.tableName == "Node") {
                for ( let d of r.data) {
                    let node:Node = d
                    if ( node.location) {
                        let loc = this.locMap.get(node.location.id)
                        if ( !loc ) {
                            loc = new LoadflowLocation(node.location)
                            this.locMap.set(loc.id,loc)
                        }
                        if ( !updateLocsMap.has(loc.id)) {
                            updateLocsMap.set(loc.id,loc);
                        }
                    }
                }
                for ( let d of r.deletedData) {
                    let node:Node = d
                    if ( node.location) {
                        let loc = this.locMap.get(node.location.id)
                        if ( loc ) {
                            if ( !updateLocsMap.has(loc.id)) {
                                updateLocsMap.set(loc.id,loc);
                            }    
                        }
                    }
                }
            }
            if ( r.tableName == "Branch") {
                for ( let d of r.data) {
                    let branch:Branch = d
                    let keys = this.getBranchKeys(branch)
                    let link: LoadflowLink | undefined
                    if ( this.linkMap.has(keys.key1) ) {
                        link = this.linkMap.get(keys.key1);
                    } else if ( this.linkMap.has(keys.key2)) {
                        link = this.linkMap.get(keys.key2);
                    }
                    if ( !link ) {
                        if ( this.isBranchExternal(branch) ) {
                            let ctrl = this.networkData.ctrls.data.find( m=>m.branchId === branch.id);
                            let isHVDC = ctrl?.type === LoadflowCtrlType.HVDC;            
                            link = new LoadflowLink(branch);
                            this.linkMap.set(keys.key1,link)    
                        }
                    }
                    //
                    if ( link ) {
                        if ( !updateLinksMap.has(link.id)) {
                            updateLinksMap.set(link.id,link);
                        }    
                    }
                }
            }
        }
        //
        let updateLocs = Array.from(updateLocsMap.values())
        let updateLinks = Array.from(updateLinksMap.values())
        // figure out what links need deleting
        let deleteLinks:LoadflowLink[] = []
        let deleteLinkKeys:string[] = []
        let extBranches = this.networkData.branches.data.filter(m=>this.isBranchExternal(m))
        for( let key of this.linkMap.keys()) {
            let link = this.linkMap.get(key)
            if ( link?.branchCount==0) {
                deleteLinks.push(link)
                deleteLinkKeys.push(key);
            }
        }
        for( let key of deleteLinkKeys) {
            this.linkMap.delete(key)
        }
        //
        this.locationData.locations = Array.from(this.locMap.values());
        this.locationData.links = Array.from(this.linkMap.values());
        //
        this.LocationDataUpdated.emit({updateLocations: updateLocs, deleteLocations: [], updateLinks: updateLinks, deleteLinks: deleteLinks, clearBeforeUpdate: false })
    }

    updateLocationData(clear: boolean) {
        let updateLocationData = { updateLocations: [], deleteLocations: [], updateLinks: [] , deleteLinks: [], clearBeforeUpdate: clear }
        if ( clear) {
            this.locMap.clear()
            this.linkMap.clear()
        }
        this.updateLocations(updateLocationData)
        this.updateLinks(updateLocationData)
        //
        this.LocationDataUpdated.emit(updateLocationData)
    }

    updateLocations(updateLocationData: UpdateLocationData) {
        //
        let updateLocs:LoadflowLocation[] = []
        let deleteLocs:LoadflowLocation[] = []
        let deleteLocKeys:number[] = []
        let locations = this.networkData.locations.data
        let nodes = this.networkData.nodes.data.filter(m=>m.location)
        let nodeMap = new Map<number,Node[]>()
        for ( let gsl of locations) {
            let ns = nodes.filter(m=>m.location?.id == gsl.id)
            nodeMap.set(gsl.id,ns)
            //
            let loc = this.locMap.get(gsl.id)
            if ( loc ) {
                loc.setLocation(gsl)
            } else {
                loc = new LoadflowLocation(gsl)
                this.locMap.set(loc.id,loc)
            }
        }
        //
        let ctrls = this.networkData.ctrls.data
        for( let key of this.locMap.keys()) {
            let loc = this.locMap.get(key)
            let nodes = nodeMap.get(key);
            if ( loc ) {
                if ( nodes ) {
                    if ( loc.update(nodes,ctrls)) {
                        updateLocs.push(loc)
                    }
                } else {
                    deleteLocs.push(loc)
                    deleteLocKeys.push(key)    
                }
            }
        }
        //
        for( let key of deleteLocKeys) {
            this.locMap.delete(key)
        }
        //
        this.locationData.locations = Array.from(this.locMap.values());

        //
        updateLocationData.updateLocations = updateLocs
        updateLocationData.deleteLocations = deleteLocs
    }

    updateLinks(updateLocationData: UpdateLocationData) {
        let updateLinks:LoadflowLink[] = []
        let deleteLinks:LoadflowLink[] = []
        let deleteLinkKeys:string[] = []
        let extBranches = this.networkData.branches.data.filter(m=>this.isBranchExternal(m))        
        let branchMap = new Map<string,Branch[]>()
        for( let b of extBranches) {
            let key = this.getBranchKeys(b)
            let val = branchMap.get(key.key1)
            if ( !val ) {
                val = []
                branchMap.set(key.key1,val)
            }
            val.push(b)
            //
            if ( !this.linkMap.has(key.key1) && !this.linkMap.has(key.key2)) {
                let link = new LoadflowLink(b)
                this.linkMap.set(key.key1,link)  
            }
        }
        //
        let ctrls = this.networkData.ctrls.data
        let branches = this.networkData.branches.data
        let ctrlMap = new Map<number,Ctrl>()
        for( let b of branches) {
            let ctrl = ctrls.find(m=>m.branchId == b.id)
            if ( ctrl ) {
                ctrlMap.set(b.id,ctrl)
            }
        }
        //
        for ( let key of this.linkMap.keys()) {
            let link = this.linkMap.get(key)
            let branches = branchMap.get(key)
            if ( link ) {
                if ( branches ) {
                    if ( link.update(branches, ctrlMap)) {
                        updateLinks.push(link)
                    }
                } else {
                    deleteLinks.push(link)
                    deleteLinkKeys.push(key)    
                }
            }
        }
        for( let key of deleteLinkKeys) {
            this.linkMap.delete(key)
        }
        //
        this.locationData.links = Array.from(this.linkMap.values());
        updateLocationData.updateLinks = updateLinks
        updateLocationData.deleteLinks = deleteLinks
    }

    private isBranchExternal(branch: Branch) {
        return branch.node1LocationId>0 && branch.node2LocationId>0 && branch.node1LocationId!=branch.node2LocationId
    }

    private getDatasetData(typeName: string):DatasetData<any> {
        if ( typeName == "Node") {
            return this.networkData.nodes;
        } else if ( typeName == "Branch") {
            return this.networkData.branches
        } else if ( typeName == "Ctrl") {
            return this.networkData.ctrls
        } else if ( typeName == "Zone") {
            return this.networkData.zones
        } else if ( typeName == "Boundary") {
            return this.networkData.boundaries
        } else if ( typeName == "GridSubstationLocation") {
            return this.networkData.locations
        } else {
            throw `Unexpected typeName found [${typeName}]`
        }
    }

    afterDelete(id: number, className: string, dataset: Dataset) {
        console.log('after Delete')
        console.log(`id=${id},className=${className},datasetId=${dataset.id}`);

        let dd = this.getDatasetData(className)
        DatasetsService.deleteDatasetData(dd,id, dataset)
        //
        this.NetworkDataLoaded.emit(this.networkData)
        //
        this.updateLocationData(false)
    }

    old_deleteLocationData(id: number, className: string, dataset: Dataset) {
        let deleteLocs:ILoadflowLocation[] = []
        let updateLocs:ILoadflowLocation[] = []
        let updateLinks:ILoadflowLink[] = []
        if ( className === "GridSubstationLocation") {
            let loc = this.locMap.get(id)
            if ( loc ) {
                deleteLocs.push(loc)
            }
        } else if ( className === "Node") {
            let locs = this.locationData.locations;
        } else if ( className === "Branch") {
            let links = this.locationData.links;
        }
        //
        for( let loc of deleteLocs) {
            this.locMap.delete(loc.id)
        }
        // figure out what links need deleting
        let deleteLinks:LoadflowLink[] = []
        let deleteLinkKeys:string[] = []
        let extBranches = this.networkData.branches.data.filter(m=>this.isBranchExternal(m))
        for( let key of this.linkMap.keys()) {
            let link = this.linkMap.get(key)
            if ( link?.branchCount==0) {
                deleteLinks.push(link)
                deleteLinkKeys.push(key);
            }
        }
        for( let key of deleteLinkKeys) {
            this.linkMap.delete(key)
        }
        //
        this.locationData.locations = Array.from(this.locMap.values());
        this.locationData.links = Array.from(this.linkMap.values());
        //
        this.LocationDataUpdated.emit({updateLocations: updateLocs, deleteLocations: deleteLocs, updateLinks: updateLinks, deleteLinks: deleteLinks, clearBeforeUpdate: false })
    }

    afterUnDelete(resp: DatasetData<any>[] ) {
        console.log('after un Delete')
        console.log(resp);

        //
        for( let r of resp) {
            let dd = this.getDatasetData(r.tableName)
            DatasetsService.updateDatasetData(dd,r)    
        }
        //
        this.NetworkDataLoaded.emit(this.networkData)
        //
        this.updateLocationData(false);
    }

    ResultsLoaded:EventEmitter<LoadflowResults> = new EventEmitter<LoadflowResults>()
    NetworkDataLoaded:EventEmitter<NetworkData> = new EventEmitter<NetworkData>()
    LocationDataLoaded:EventEmitter<LocationData> = new EventEmitter<LocationData>()
    LocationDataUpdated:EventEmitter<UpdateLocationData> = new EventEmitter<UpdateLocationData>()
    AllTripsProgress:EventEmitter<any> = new EventEmitter<any>()
    ObjectSelected:EventEmitter<SelectedMapItem> = new EventEmitter<SelectedMapItem>()

}

export class LoadflowLocation implements ILoadflowLocation {
    private _gsl: GridSubstationLocation
    private _isQB: boolean
    private _hasNodes: boolean
    private _isNew: boolean

    constructor(loc:GridSubstationLocation) {
        this._gsl = loc
        this._isQB = false
        this._hasNodes = false
        this._isNew = true;
    }

    setLocation(gsl: GridSubstationLocation) {
        // this forces and update if they have changed
        this._isNew = this.areGslDifferent(gsl,this._gsl)
        this._gsl = gsl
    }

    get id():number {
        return this._gsl.id
    }

    get name():string {
        return this._gsl.name
    }

    get reference():string {
        return this._gsl.reference
    }

    get gisData():GISData {
        return { id: 0, latitude: this._gsl.latitude, longitude: this._gsl.longitude}
    }

    get longitude():number {
        return this._gsl.longitude;
    }

    get latutude():number {
        return this._gsl.longitude
    }

    get isQB():boolean {
        return this._isQB
    }

    get hasNodes():boolean {
        return this._hasNodes
    }

    update(nodes: Node[], ctrls: Ctrl[]):boolean {
        let hasNodes = nodes.length>0
        let isQB = ctrls.find(m=>m.node1.location?.id === this._gsl.id && m.type == LoadflowCtrlType.QB)!==undefined
        let result = hasNodes!=this._hasNodes || 
                        isQB!=this._isQB || 
                        this._isNew 
        this._hasNodes = hasNodes
        this._isQB = isQB
        this._isNew = false
        return result
    }

    private areGslDifferent(gslA: GridSubstationLocation, gslB: GridSubstationLocation) {
        return gslA.latitude!=gslB.latitude || gslA.longitude != gslB.longitude
    }

}

export class LoadflowLink implements ILoadflowLink {

    private static idCounter:number = 0

    private _node1LocationId:number = 0
    private _node2LocationId:number = 0
    private _gisData1:GISData = { id:0, latitude: 0, longitude: 0}
    private _gisData2:GISData = { id:0, latitude: 0, longitude: 0}
    private _voltage:number = 0
    private _id:number
    private _isNew: boolean

    constructor(branch:Branch) {
        this._node1LocationId = branch.node1LocationId;
        this._node2LocationId = branch.node2LocationId;
        this._voltage = branch.node1Voltage;
        if ( branch.node1GISData) {
            this._gisData1 = branch.node1GISData;
        }
        if ( branch.node2GISData) {
            this._gisData2 = branch.node2GISData
        }

        this.isHVDC = false
        this.branchCount = 1
        LoadflowLink.idCounter++
        this._id = LoadflowLink.idCounter
        this._isNew = true
    }

    get id():number {
        return this._id;
    }

    get node1LocationId():number {
        return this._node1LocationId;
    }

    get node2LocationId():number {
        return this._node2LocationId;
    }

    get voltage():number {
        return this._voltage
    }

    get gisData1():GISData {
        return this._gisData1
    }

    get gisData2():GISData  {
        return this._gisData2
    }

    update(branches: Branch[],ctrlMap: Map<number,Ctrl>):boolean {
        let isHVDC = false
        branches.forEach(m=>{
            let ctrl = ctrlMap.get(m.id)
            if ( ctrl?.type === LoadflowCtrlType.HVDC) {
                isHVDC = true
            }
        })
        //
        let node1LocationId = branches[0].node1LocationId
        let node2LocationId = branches[0].node2LocationId
        let gisData1 = branches[0].node1GISData ? branches[0].node1GISData : { id:0, latitude: 0, longitude: 0}
        let gisData2 = branches[0].node2GISData ? branches[0].node2GISData : { id:0, latitude: 0, longitude: 0}
        //
        let result = branches.length!==this.branchCount || 
                        isHVDC!==this.isHVDC || 
                        node1LocationId !== this.node1LocationId ||
                        node2LocationId !== this.node2LocationId ||
                        this.areGISDataDifferent(gisData1,this._gisData1) ||
                        this.areGISDataDifferent(gisData2,this._gisData2) ||
                        this._isNew
        this._node1LocationId = node1LocationId
        this._node2LocationId = node2LocationId
        this._gisData1 = gisData1
        this._gisData2 = gisData2
        this.isHVDC = isHVDC
        this.branchCount = branches.length
        this._isNew = false

        return result
    }

    private areGISDataDifferent(gisA: GISData, gisB: GISData) {
        return gisA.latitude!=gisB.latitude || gisA.longitude!=gisB.longitude
    }

    branchCount:number
    isHVDC: boolean
}
