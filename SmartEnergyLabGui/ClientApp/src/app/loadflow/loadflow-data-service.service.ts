import { EventEmitter, Injectable } from '@angular/core';
import { Boundary, GridSubstation, LoadflowLink, LoadflowLocation, LoadflowResults, LocationData, NetworkData, Node, NodeWrapper } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';
import { SignalRService } from '../main/signal-r-status/signal-r.service';

type NodeDict = {
    [code: string]:Node
}

export type SelectedMapItem = {location: LoadflowLocation | null, link: LoadflowLink | null}

@Injectable({
    providedIn: 'root'
})

export class LoadflowDataService {

    constructor(private dataClientService: DataClientService, private signalRService: SignalRService) { 
        this.gridSubstations = [];
        this.boundaries = [];
        this.networkData = { nodes: [], branches: [], ctrls: [] }
        this.locationData = { locations: [], links: []}
        dataClientService.GetBoundaries((results)=>{
            this.boundaries = results;
            this.BoundariesLoaded.emit(results);
        });
        dataClientService.GetNetworkData( (results)=>{
            this.networkData = results;
            this.NetworkDataLoaded.emit(results);
        })
        dataClientService.GetLocationData( (results)=>{
            this.locationData = results;
            this.LocationDataLoaded.emit(results);
        })
        this.signalRService.hubConnection.on('Loadflow_AllTripsProgress', (data) => {
            this.AllTripsProgress.emit(data);
        })
        //
        this.selectedMapItem = null
    }

    boundaries: Boundary[]
    gridSubstations: GridSubstation[]
    networkData: NetworkData
    locationData: LocationData
    loadFlowResults: LoadflowResults | undefined
    boundaryName: string | undefined

    selectedMapItem: SelectedMapItem | null

    setBound(boundaryName: string) {
        this.dataClientService.SetBound(boundaryName, (results) =>{
            this.boundaryName = boundaryName
            this.loadFlowResults = results
            this.ResultsLoaded.emit(results)
        })
    }

    runBaseLoadflow() {
        this.dataClientService.RunBaseLoadflow( (results) => {
            this.loadFlowResults = results;
            this.ResultsLoaded.emit(results);
        });
    }

    runBoundaryTrip(boundaryName:string, tripName: string) {
        this.dataClientService.RunBoundaryTrip( boundaryName, tripName, (results) => {
            this.loadFlowResults = results;
            this.ResultsLoaded.emit(results);
        });
    }

    runAllBoundaryTrips(boundaryName:string) {
        this.dataClientService.RunAllBoundaryTrips( boundaryName, (results) => {
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

    BoundariesLoaded:EventEmitter<Boundary[]> = new EventEmitter<Boundary[]>()
    ResultsLoaded:EventEmitter<LoadflowResults> = new EventEmitter<LoadflowResults>()
    NetworkDataLoaded:EventEmitter<NetworkData> = new EventEmitter<NetworkData>()
    LocationDataLoaded:EventEmitter<LocationData> = new EventEmitter<LocationData>()
    AllTripsProgress:EventEmitter<any> = new EventEmitter<any>()
    ObjectSelected:EventEmitter<SelectedMapItem> = new EventEmitter<SelectedMapItem>()

}
