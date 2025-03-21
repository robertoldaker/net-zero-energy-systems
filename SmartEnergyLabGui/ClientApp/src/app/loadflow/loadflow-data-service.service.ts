import { EventEmitter, Injectable } from '@angular/core';
import { Branch, Ctrl, Dataset, DatasetData, DatasetType, GISData, GridSubstation, GridSubstationLocation, ILoadflowLink, ILoadflowLocation, LoadflowCtrlType, LoadflowResults, LocationData, NetworkData, Node, TransportModel, UpdateLocationData } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';
import { SignalRService } from '../main/signal-r-status/signal-r.service';
import { ShowMessageService } from '../main/show-message/show-message.service';
import { DialogService } from '../dialogs/dialog.service';
import { MessageDialog } from '../dialogs/message-dialog/message-dialog.component';
import { DatasetsService } from '../datasets/datasets.service';
import { DataFilter, ICellEditorDataDict } from '../datasets/cell-editor/cell-editor.component';

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
        this.signalRService.hubConnection.on('BoundCalc_AllTripsProgress', (data) => {
            this.AllTripsProgress.emit(data);
        })
        //
        this.selectedMapItem = null
    }

    dataset: Dataset = {id: 0, type: DatasetType.BoundCalc, name: '', parent: null, isReadOnly: true}
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
            this.messageService.clearMessage()
            this.loadFlowResults = undefined
            this.NetworkDataLoaded.emit(results)
            this.updateLocationData(true)
        })
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

    runBoundCalc(transportModel: TransportModel, boundaryName: string, boundaryTrips: boolean, tripStr: string) {
        this.inRun = true;
        this.dataClientService.RunBoundCalc( this.dataset.id, transportModel, boundaryName, boundaryTrips, tripStr, (results) => {
            this.inRun = false;
            this.loadFlowResults = results;
            this.ResultsLoaded.emit(results);
        });
    }

    adjustBranchCapacities(transportModel: TransportModel) {
        this.inRun = true;
        this.dataClientService.AdjustBranchCapacities( this.dataset.id, transportModel, (results) => {
            this.inRun = false;
            this.loadFlowResults = results;
            this.ResultsLoaded.emit(results);
        });
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

    selectLocationByName(locName: string) {
        let loc = this.locationData.locations.find(m=>m.name == locName)
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

    selectLinkByLocIds(node1LocationId: number, node2LocationId: number) {
        let branch = this.locationData.links.find(m=>m.node1LocationId == node1LocationId && m.node2LocationId == node2LocationId)
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
        let lowerStr = str.toLocaleLowerCase()
        let upperStr = str.toUpperCase()
        var searchResults = this.locationData.locations.
            filter(m=>m.name && m.name.toLocaleLowerCase().includes(lowerStr) || m.reference === upperStr).
            slice(0,maxResults)
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

    /*
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
    */

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
        //
        console.log('afterEdit',resp)
        for( let r of resp) {
            let dd = this.getDatasetData(r.tableName)
            DatasetsService.updateDatasetData(dd,r)    
        }
        //
        if ( this.loadFlowResults) {
            console.log('afterEdit results',this.loadFlowResults)
            this.ResultsLoaded.emit(this.loadFlowResults)
        } else {
            console.log('afterEdit networkData',this.networkData)
            this.NetworkDataLoaded.emit(this.networkData)
        }
        //
        this.updateLocationData(false);
    }

    afterDelete(id: number, className: string, dataset: Dataset) {

        let dd = this.getDatasetData(className)
        if ( className === "Branch") {
            let branch = dd.data.find(m=>m.id == id)
            if ( branch && branch.ctrlId!==0) {
                DatasetsService.deleteDatasetData(this.networkData.ctrls,branch.ctrlId, dataset)
            }
        } 
        DatasetsService.deleteDatasetData(dd,id, dataset)
        //
        this.NetworkDataLoaded.emit(this.networkData)
        //
        this.updateLocationData(false)
    }

    afterUnDelete(resp: DatasetData<any>[] ) {
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
            return this.loadFlowResults ? this.loadFlowResults.nodes : this.networkData.nodes;
        } else if ( typeName == "Branch") {
            return this.loadFlowResults ? this.loadFlowResults.branches : this.networkData.branches
        } else if ( typeName == "Ctrl") {
            return this.loadFlowResults ? this.loadFlowResults.ctrls : this.networkData.ctrls
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

    public getBranchEditorData(branchId: number):IBranchEditorData {
        let ctrlData
        let branchData
        let df = new DataFilter(1)
        let branch = this.networkData.branches.data.find(m=>m.id === branchId)
        if ( branch ) {
            let branchDataset = { 
                tableName: this.networkData.branches.tableName, 
                data: [branch],
                deletedData: [],
                userEdits: this.networkData.branches.userEdits
                }
            let branches = df.GetCellDataObjects(this.dataset,branchDataset,(item)=>item.id.toString())
            branchData = branches[0]
            let ctrlId = branch.ctrlId   
            if ( ctrlId!=0 ) {
                let ctrl = this.networkData.ctrls.data.find(m=>m.id === ctrlId)
                if ( ctrl ) {
                    let ctrlDataset = { tableName: this.networkData.ctrls.tableName, 
                        data: [ctrl],
                        deletedData: [],
                        userEdits: this.networkData.ctrls.userEdits
                        }
                    let ctrls = df.GetCellDataObjects(this.dataset,ctrlDataset,(item)=>item.id.toString())
                    ctrlData = ctrls[0]    
                }
            }
        } else {
            throw `Cannot find branch with id [${branchId}]`
        }
        return { branch: branchData, ctrl: ctrlData}
    }    

    public getNodeEditorData(nodeId: number):ICellEditorDataDict {
        let df = new DataFilter(1)
        let node = this.networkData.nodes.data.find(m=>m.id === nodeId)
        if ( node ) {
            let branchDataset = { 
                tableName: this.networkData.nodes.tableName, 
                data: [node],
                deletedData: [],
                userEdits: this.networkData.nodes.userEdits
                }
            let nodes = df.GetCellDataObjects(this.dataset,branchDataset,(item)=>item.id.toString())
            return nodes[0]
         } else {
            throw `Cannot find node with id [${nodeId}]`
        }
    }    

    ResultsLoaded:EventEmitter<LoadflowResults> = new EventEmitter<LoadflowResults>()
    NetworkDataLoaded:EventEmitter<NetworkData> = new EventEmitter<NetworkData>()
    LocationDataUpdated:EventEmitter<UpdateLocationData> = new EventEmitter<UpdateLocationData>()
    AllTripsProgress:EventEmitter<any> = new EventEmitter<any>()
    ObjectSelected:EventEmitter<SelectedMapItem> = new EventEmitter<SelectedMapItem>()

}

export interface IBranchEditorData {
    branch: ICellEditorDataDict,
    ctrl: ICellEditorDataDict | undefined
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
        this._id = branch.id
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
