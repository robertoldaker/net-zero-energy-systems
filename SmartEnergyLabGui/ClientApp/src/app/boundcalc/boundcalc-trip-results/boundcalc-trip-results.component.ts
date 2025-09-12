import { AfterViewInit, Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { AllTripResult } from '../../data/app.data';
import { BoundCalcDataService } from '../boundcalc-data-service.service';

@Component({
    selector: 'app-boundcalc-trip-results',
    templateUrl: './boundcalc-trip-results.component.html',
    styleUrls: ['./boundcalc-trip-results.component.css']
})
export class BoundCalcTripResultsComponent implements OnInit, OnDestroy, AfterViewInit {

    subs1: Subscription
    constructor(private dataService: BoundCalcDataService) { 
        if ( dataService.loadFlowResults?.boundaryTripResults ) {
            this.singleTrips = this.createDataSource(dataService.loadFlowResults.boundaryTripResults.singleTrips)
            this.doubleTrips = this.createDataSource(dataService.loadFlowResults.boundaryTripResults.doubleTrips)
            this.intactTrips = this.createDataSource(dataService.loadFlowResults.boundaryTripResults.intactTrips)
        } else {
            this.singleTrips = this.createDataSource([])
            this.doubleTrips = this.createDataSource([])
            this.intactTrips = this.createDataSource([])
        }
        this.subs1 = dataService.ResultsLoaded.subscribe( (results) => {
            if ( results.boundaryTripResults?.singleTrips ) {
                this.singleTrips = this.createDataSource(results.boundaryTripResults.singleTrips)
            }
            if ( results.boundaryTripResults?.doubleTrips ) {
                this.doubleTrips = this.createDataSource(results.boundaryTripResults.doubleTrips)
            }
            if ( results.boundaryTripResults?.intactTrips ) {
                this.intactTrips = this.createDataSource(results.boundaryTripResults.intactTrips)
            }
        })
    }

    ngAfterViewInit(): void {
       
    }

    createDataSource(items: AllTripResult[]): MatTableDataSource<AllTripResult> {
        let ds = new MatTableDataSource(items);
        return ds;
    }
    ngOnDestroy(): void {
        this.subs1.unsubscribe();
    }

    ngOnInit(): void {

    }

    tabChange() {
        // needed for the div-auto-scroller to get the right size
        window.dispatchEvent(new Event('resize'));        
    }

    singleTrips: MatTableDataSource<AllTripResult>
    doubleTrips: MatTableDataSource<AllTripResult>
    intactTrips: MatTableDataSource<AllTripResult>

}
