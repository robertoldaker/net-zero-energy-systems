import { EventEmitter, Injectable } from '@angular/core';
import { Branch, Dataset, DatasetData, DatasetType, GISData, GridSubstation, GridSubstationLocation, ILoadflowLink, ILoadflowLocation, LoadflowCtrlType, LoadflowResults, LocationData, NetworkData, Node, UpdateLocationData } from '../data/app.data';
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
            this.locationData = this.getLocationData(this.networkData)
            this.messageService.clearMessage()
            this.NetworkDataLoaded.emit(results);
            this.LocationDataLoaded.emit(this.locationData);
            // re-select if location is still selected
            if ( this.selectedMapItem?.location ) {
                let oldLoc = this.selectedMapItem.location
                this.selectLocation(oldLoc.id)
            }
            // and link
            if ( this.selectedMapItem?.link ) {
                let oldLink = this.selectedMapItem.link
                this.selectLink(oldLink.id)
            }
        })
    }

    private getLocationData(nd: NetworkData):LocationData {
        //
        let locs = nd.locations.data
        let ctrls = nd.ctrls.data
        let nodes = nd.nodes.data
        let branches = nd.branches.data
        //
        this.locMap = new Map<number,LoadflowLocation>()
        //
        for( let loc of locs) {
            let isQB = ctrls.find(m=>m.node1.location?.id == loc.id && m.type == LoadflowCtrlType.QB)!==undefined
            this.locMap.set(loc.id,new LoadflowLocation(loc, isQB));
        }  
        // ids of locations that have ctrls
        for(let node of nodes) {
            let loc: LoadflowLocation | undefined;
            if ( node.location && this.locMap.has(node.location.id)) {
                loc = this.locMap.get(node.location.id);
                loc?.nodes.push(node);
            } 
        }
        let locations = Array.from(this.locMap.values());

        // Links - include branches that connect different locations
        let visibleBranches = branches.filter( 
            m=>m.node1LocationId>0 && 
            m.node2LocationId>0 && 
            m.node1LocationId != m.node2LocationId);
        this.linkMap = new Map<string,LoadflowLink>();
        for( let b of visibleBranches) {
            let link:LoadflowLink | undefined;
            let keys = this.getBranchKeys(b);
            if ( this.linkMap.has(keys.key1) ) {
                link = this.linkMap.get(keys.key1);
                link?.branches.push(b);
            } else if ( this.linkMap.has(keys.key2)) {
                link = this.linkMap.get(keys.key2);
                link?.branches.push(b);
            } else {
                let ctrl = ctrls.find( m=>m.branchId === b.id);
                let isHVDC = ctrl?.type === LoadflowCtrlType.HVDC;
                link = new LoadflowLink(b,isHVDC);
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

    afterEdit(resp: DatasetData<any>[] ) {
        //
        for( let r of resp) {
            let dd = this.getDatasetData(r.tableName)
            DatasetsService.updateDatasetData(dd,r)    
        }
        //
        this.NetworkDataLoaded.emit(this.networkData)
        //
        this.updateLocationData(resp);
    }

    updateLocationData( resp: DatasetData<any>[]) {
        let updateLocsMap:Map<number,ILoadflowLocation> = new Map<number,ILoadflowLocation>()
        let updateLinksMap:Map<number,ILoadflowLink> = new Map<number,ILoadflowLink>()
        for( let r of resp) {
            if ( r.tableName == "GridSubstationLocation") {
                for ( let d of r.data) {
                    let gsl:GridSubstationLocation = d
                    let loc = this.locMap.get(gsl.id)
                    if ( loc ) {
                        loc.setLocation(gsl)
                    } else {
                        loc = new LoadflowLocation(gsl,false)
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
                            loc = new LoadflowLocation(node.location,false)
                            this.locMap.set(loc.id,loc)
                        }
                        loc.updateNode(node)
                        if ( !updateLocsMap.has(loc.id)) {
                            updateLocsMap.set(loc.id,loc);
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
                    if ( link ) {
                        let updated = link.updateBranch(branch);
                        if ( !updated ) {
                            let links = Array.from(this.linkMap.values())
                            links.find( m=>m.removeBranch(branch))
                            link.branches.push(branch)                            
                        }
                    } else {
                        let links = Array.from(this.linkMap.values())
                        links.find( m=>m.removeBranch(branch))
                        link = new LoadflowLink(branch,false);
                        this.linkMap.set(keys.key1,link)
                    }
                    //
                    if ( !updateLinksMap.has(link.id)) {
                        updateLinksMap.set(link.id,link);
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
        for( let key of this.linkMap.keys()) {
            let link = this.linkMap.get(key)
            if ( link?.branches.length==0) {
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
        this.LocationDataUpdated.emit({updateLocations: updateLocs, deleteLocations: [], updateLinks: updateLinks, deleteLinks: deleteLinks })
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
        this.NetworkDataLoaded.emit(this.networkData)
        //
        this.deleteLocationData(id,className,dataset)
    }

    deleteLocationData(id: number, className: string, dataset: Dataset) {
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
            let loc = locs.find(m=>m.nodes.findIndex(n=>n.id==id)>=0);
            if ( loc ) {
                let index = loc.nodes.findIndex(m=>m.id==id);
                loc.nodes.splice(index,1)
                updateLocs.push(loc)
            }
        } else if ( className === "Branch") {
            let links = this.locationData.links;
            let link = links.find(m=>m.branches.findIndex(n=>n.id==id)>=0);
            if ( link ) {
                let index = link.branches.findIndex(m=>m.id==id);
                link.branches.splice(index,1)
                if ( link.branches.length>0) {
                    updateLinks.push(link)
                }
            }
        }
        //
        for( let loc of deleteLocs) {
            this.locMap.delete(loc.id)
        }
        // figure out what links need deleting
        let deleteLinks:LoadflowLink[] = []
        let deleteLinkKeys:string[] = []
        for( let key of this.linkMap.keys()) {
            let link = this.linkMap.get(key)
            if ( link?.branches.length==0) {
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
        this.LocationDataUpdated.emit({updateLocations: updateLocs, deleteLocations: deleteLocs, updateLinks: updateLinks, deleteLinks: deleteLinks })
    }

    afterUnDelete(id: number, className: string, dataset: Dataset) {
        console.log('after un Delete')
        console.log(`id=${id},className=${className},datasetId=${dataset.id}`);

        let dd = this.getDatasetData(className)
        DatasetsService.unDeleteDatasetData(dd,id, dataset)
        this.NetworkDataLoaded.emit(this.networkData)
    }


    ResultsLoaded:EventEmitter<LoadflowResults> = new EventEmitter<LoadflowResults>()
    NetworkDataLoaded:EventEmitter<NetworkData> = new EventEmitter<NetworkData>()
    LocationDataLoaded:EventEmitter<LocationData> = new EventEmitter<LocationData>()
    LocationDataUpdated:EventEmitter<UpdateLocationData> = new EventEmitter<UpdateLocationData>()
    AllTripsProgress:EventEmitter<any> = new EventEmitter<any>()
    ObjectSelected:EventEmitter<SelectedMapItem> = new EventEmitter<SelectedMapItem>()

}

export class LoadflowLocation implements ILoadflowLocation {
    private _gsl: GridSubstationLocation;
    private _nodes: Node[];
    private _isQB: boolean;

    constructor(loc:GridSubstationLocation, isQB: boolean) {
        this._nodes = []
        this._gsl = loc;
        this._isQB = isQB;
    }

    setLocation(gsl: GridSubstationLocation) {
        this._gsl = gsl;
    }

    updateNode(node: Node) {
        let index = this._nodes.findIndex(m=>m.id == node.id)
        if ( index>=0 ) {
            this._nodes[index] = node;
        } else {
            this._nodes.push(node);
        }
    }

    get id():number {
        return this._gsl.id;
    }

    get name():string {
        return this._gsl.name;
    }

    get nodes():Node[] {
        return this._nodes;
    }

    get reference():string {
        return this._gsl.reference;
    }

    get gisData():GISData {
        return { id: 0, latitude: this._gsl.latitude, longitude: this._gsl.longitude};
    }

    get longitude():number {
        return this._gsl.longitude;
    }

    get latutude():number {
        return this._gsl.longitude;
    }

    get isQB():boolean {
        return this._isQB;
    }

}

export class LoadflowLink implements ILoadflowLink {

    private static idCounter:number = 0;

    private _branches:Branch[]
    private _id:number;

    constructor(branch:Branch, isHVDC: boolean) {
        this._branches = []
        this._branches.push(branch)
        this.isHVDC = isHVDC
        LoadflowLink.idCounter++
        this._id = LoadflowLink.idCounter
    }

    get id():number {
        return this._id;
    }

    get branches():Branch[] {
        return this._branches;
    }

    get voltage():number {
        return this._branches.length>0 ? this._branches[0].node1Voltage : 0;
    }

    get gisData1():GISData {
        if ( this._branches.length>0 && this._branches[0].node1GISData!=null) {
            return this._branches[0].node1GISData;
        } else {
            return {id: 0, latitude: 0, longitude: 0}
        }        
    }

    get gisData2():GISData  {
        if ( this.branches.length>0 && this._branches[0].node2GISData!=null) {
            return this._branches[0].node2GISData;
        } else {
            return {id: 0, latitude: 0, longitude: 0};
        }
    }

    updateBranch(branch: Branch):boolean {
        let index = this._branches.findIndex(m=>m.id == branch.id)
        if ( index>=0 ) {
            this._branches[index] = branch
            return true
        } else {
            return false
        }
    }

    removeBranch(branch: Branch):boolean {
        let index = this._branches.findIndex(m=>m.id == branch.id)
        if ( index>=0 ) {
            this._branches.splice(index,1);
            return true
        } else {
            return false
        }
    }

    isHVDC: boolean
}
