import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { Boundary, BoundaryFlowResult, BoundaryTrip, Branch, Dataset, DatasetType, LoadflowResults, TransportModel } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { DataClientService } from 'src/app/data/data-client.service';

@Component({
    selector: 'app-loadflow-dialog',
    templateUrl: './loadflow-dialog.component.html',
    styleUrls: ['./loadflow-dialog.component.css']
})

export class LoadflowDialogComponent extends ComponentBase {

    constructor(private dataService: LoadflowDataService) { 
        super()
        this.boundaries = dataService.networkData.boundaries.data;
        this.branches = dataService.networkData.branches.data;
        this.trips = []
        this.boundaryName="Unspecified"        
        this.selectedTrip=""
        this.currentTrip="";
        this.percent = 0;
        this.flowResult = this.clearFlowResult;
        this.addSub(dataService.NetworkDataLoaded.subscribe((results=>{
            this.boundaries = results.boundaries.data
            this.branches = results.branches.data
        })))
        this.addSub(dataService.ResultsLoaded.subscribe((results)=>{
            if ( results.boundaryTrips ) {
                this.selectedTrip = "";
                this.trips = results.boundaryTrips.trips                
            } 
            if ( results.boundaryFlowResult ) {
                this.flowResult = results.boundaryFlowResult;
            }
        }))
        this.addSub(dataService.AllTripsProgress.subscribe((data)=>{
            this.currentTrip = data.msg;
            this.percent = data.percent; 
        }))
        this.addSub(dataService.NetworkDataLoaded.subscribe((results)=>{
            this.flowResult = this.clearFlowResult
        }))
    }

    calc() {
        let bn:string = this.boundaryName;
        if ( bn == "Unspecified") {
            bn = "";
        }
        this.dataService.runBoundCalc(this.transportModel,bn,this.boundaryTrips,"");
    }

    runBaseLoadflow() {
        this.dataService.runBaseLoadflow();
    }

    setBound() {
        //??this.dataService.setBound(this.boundaryName);
    }

    runSingleTrip() {
        this.dataService.runBoundaryTrip(this.boundaryName, this.selectedTrip);
    }

    runAllTrips() {
        this.dataService.runAllBoundaryTrips(this.boundaryName);
    }

    getLineNames(trip: BoundaryTrip):string {
        let str = trip.lineNames[0];
        if ( trip.lineNames.length>1) {
            str+=' ' + trip.lineNames[1];
        }
        return str;
    }
    
    tripSelected(e: any) {
        let trip = this.trips.find( m=>m.text==e.value);
    }

    onDatasetSelected(dataset: Dataset) {
        this.dataService.setDataset(dataset)
    }

    transportModelChanged(e: any) {
        console.log('transport model changed')
        this.transportModel = e.value;
    }

    currentTrip: string
    percent: number
    selectedTrip: string
    boundaries: Boundary[]
    branches: any;
    boundaryName: string
    trips: BoundaryTrip[]
    flowResult: BoundaryFlowResult
    clearFlowResult: BoundaryFlowResult = { genInside: 0, genOutside:0, demInside: 0, demOutside: 0, ia: 0 }
    datasetTypes = DatasetType
    boundaryTrips = true
    transportModel: TransportModel = TransportModel.PeakSecurity
    TransportModel = TransportModel

}
