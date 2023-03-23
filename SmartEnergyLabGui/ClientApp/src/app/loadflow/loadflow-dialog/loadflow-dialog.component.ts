import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { Boundary, BoundaryFlowResult, BoundaryTrip, LoadflowResults } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';

@Component({
    selector: 'app-loadflow-dialog',
    templateUrl: './loadflow-dialog.component.html',
    styleUrls: ['./loadflow-dialog.component.css']
})

export class LoadflowDialogComponent implements OnInit, OnDestroy {

    private subs1: Subscription
    private subs2: Subscription
    private subs3: Subscription
    constructor(private dataService: LoadflowDataService) { 
        this.boundaries = dataService.boundaries;
        this.trips = []
        this.boundaryName=""        
        this.selectedTrip=""
        this.currentTrip="";
        this.percent = 0;
        this.flowResult = { genInside: 0, genOutside:0, demInside: 0, demOutside: 0, ia: 0 }
        this.subs1 = dataService.BoundariesLoaded.subscribe((results=>{
            this.boundaries = results;
        }))
        this.subs2 = dataService.ResultsLoaded.subscribe((results)=>{
            if ( results.boundaryTrips ) {
                this.selectedTrip = "";
                this.trips = results.boundaryTrips.trips                
            } 
            if ( results.boundaryFlowResult ) {
                this.flowResult = results.boundaryFlowResult;
            }
        })
        this.subs3 = dataService.AllTripsProgress.subscribe((data)=>{
            this.currentTrip = data.trip.text;
            this.percent = data.percent; 
        });
    }
    ngOnDestroy(): void {
        this.subs1.unsubscribe();
        this.subs2.unsubscribe();
        this.subs3.unsubscribe();
    }

    ngOnInit(): void {
    }

    runBaseLoadflow() {
        this.dataService.runBaseLoadflow();
    }

    setBound() {
        this.dataService.setBound(this.boundaryName);
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

    currentTrip: string
    percent: number
    selectedTrip: string
    boundaries: Boundary[]
    boundaryName: string
    trips: BoundaryTrip[]
    flowResult: BoundaryFlowResult

}
