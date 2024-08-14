import { EventEmitter, Injectable } from '@angular/core';
import { Boundary, Branch, Dataset, DatasetData, DatasetType, GridSubstation, LoadflowLink, LoadflowLocation, LoadflowResults, LocationData, NetworkData, Node } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';
import { SignalRService } from '../main/signal-r-status/signal-r.service';
import { ShowMessageService } from '../main/show-message/show-message.service';
import { DialogService } from '../dialogs/dialog.service';
import { MessageDialog } from '../dialogs/message-dialog/message-dialog.component';
import { DatasetsService } from '../datasets/datasets.service';

type NodeDict = {
    [code: string]:Node
}

export type SelectedMapItem = {location: LoadflowLocation | null, link: LoadflowLink | null}

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
            mapData: {  locations:[], links: [] }
        }
        this.locationData = { locations: [], links: []}
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
        //this.dataClientService.GetLocationData(this.dataset.id, (results)=>{
        //    this.locationData = results;
        //    this.LocationDataLoaded.emit(results);
        //})
    }

    private loadNetworkData(withMessage: boolean) {
        if ( withMessage ) {
            this.messageService.showMessage('Loading ...')
        }
        this.dataClientService.GetNetworkData( this.dataset.id, (results)=>{
            this.networkData = results
            this.locationData = results.mapData
            this.messageService.clearMessage()
            this.NetworkDataLoaded.emit(results);
            this.LocationDataLoaded.emit(results.mapData);
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

    private getLocationData(nd: NetworkData) {
        
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

    getLocation(locId: number):LoadflowLocation | undefined {
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

    searchLocations(str: string, maxResults: number):LoadflowLocation[]  {
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
        console.log('after edit')
        console.log(resp)
        //
        for( let r of resp) {
            let dd = this.getDatasetData(r.tableName)
            DatasetsService.updateDatasetData(dd,r)    
        }
        //
        this.NetworkDataLoaded.emit(this.networkData)
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
    AllTripsProgress:EventEmitter<any> = new EventEmitter<any>()
    ObjectSelected:EventEmitter<SelectedMapItem> = new EventEmitter<SelectedMapItem>()

}

