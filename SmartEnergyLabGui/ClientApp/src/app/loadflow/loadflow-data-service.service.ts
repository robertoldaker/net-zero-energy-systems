import { EventEmitter, Injectable } from '@angular/core';
import { Boundary, LoadflowResults, NetworkData } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';
import { SignalRService } from '../main/signal-r-status/signal-r.service';

@Injectable({
    providedIn: 'root'
})
export class LoadflowDataService {

    constructor(private dataClientService: DataClientService, private signalRService: SignalRService) { 
        this.boundaries = [];
        this.networkData = { nodes: [], branches: [], ctrls: [] }
        dataClientService.GetBoundaries((results)=>{
            this.boundaries = results;
            this.BoundariesLoaded.emit(results);
        });
        dataClientService.GetNetworkData( (results)=>{
            this.networkData = results;
            this.NetworkDataLoaded.emit(results);
        })
        this.signalRService.hubConnection.on('Loadflow_AllTripsProgress', (data) => {
            this.AllTripsProgress.emit(data);
        })
    }

    boundaries: Boundary[]
    networkData: NetworkData
    loadFlowResults: LoadflowResults | undefined

    setBound(boundaryName: string) {
        this.dataClientService.SetBound(boundaryName, (results) =>{
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

    BoundariesLoaded:EventEmitter<Boundary[]> = new EventEmitter<Boundary[]>()
    ResultsLoaded:EventEmitter<LoadflowResults> = new EventEmitter<LoadflowResults>()
    NetworkDataLoaded:EventEmitter<NetworkData> = new EventEmitter<NetworkData>()
    AllTripsProgress:EventEmitter<any> = new EventEmitter<any>()

}
