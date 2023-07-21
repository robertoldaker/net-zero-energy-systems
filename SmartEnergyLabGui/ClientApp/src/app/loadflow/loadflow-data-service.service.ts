import { EventEmitter, Injectable } from '@angular/core';
import { Boundary, GridSubstation, LoadflowResults, LocationData, NetworkData, Node, NodeWrapper } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';
import { SignalRService } from '../main/signal-r-status/signal-r.service';

type NodeDict = {
    [code: string]:Node
}

@Injectable({
    providedIn: 'root'
})
export class LoadflowDataService {

    constructor(private dataClientService: DataClientService, private signalRService: SignalRService) { 
        this.gridSubstations = [];
        this.boundaries = [];
        this.networkData = { nodes: [], branches: [], ctrls: [] }
        this.locationData = { locations: [], branches: []}
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
    }

    boundaries: Boundary[]
    gridSubstations: GridSubstation[]
    networkData: NetworkData
    locationData: LocationData
    loadFlowResults: LoadflowResults | undefined

    private addBranchAndCtrlNodes() {
        let nodeDict:NodeDict = {}
        let nodes = this.networkData.nodes;
        let branches = this.networkData.branches;
        let ctrls = this.networkData.ctrls;
        //
        branches.forEach(bw=>{
            let b = bw.obj
            if ( nodeDict[b.node1Code] ) {
                b.node1 = nodeDict[b.node1Code]
            } else {
                let nw = this.getNode(b.node1Code);
                if ( nw) {
                    b.node1 = nw.obj;
                    nodeDict[b.node1Code] = b.node1    
                }
            }
            if ( nodeDict[b.node2Code] ) {
                b.node2 = nodeDict[b.node2Code]
            } else {
                let nw = this.getNode(b.node2Code);
                if ( nw) {
                    b.node2 = nw.obj
                    nodeDict[b.node2Code] = b.node2    
                }
            }
        })

        ctrls.forEach(cw=>{
            let c = cw.obj
            if ( nodeDict[c.node1Code] ) {
                c.node1 = nodeDict[c.node1Code]
            } else {
                let nw = this.getNode(c.node1Code);
                if ( nw) {
                    c.node1 = nw.obj;
                    nodeDict[c.node1Code] = c.node1    
                }
            }
            if ( nodeDict[c.node2Code] ) {
                c.node2 = nodeDict[c.node2Code]
            } else {
                let nw = this.getNode(c.node2Code);
                if ( nw) {
                    c.node2 = nw.obj
                    nodeDict[c.node2Code] = c.node2    
                }
            }
        })
    }

    private getNode(code: string):NodeWrapper | undefined {
        return this.networkData.nodes.find(m=>m.obj.code==code)
    }

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
    LocationDataLoaded:EventEmitter<LocationData> = new EventEmitter<LocationData>()
    AllTripsProgress:EventEmitter<any> = new EventEmitter<any>()

}
