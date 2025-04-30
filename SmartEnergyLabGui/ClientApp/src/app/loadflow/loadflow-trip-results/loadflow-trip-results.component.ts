import { AfterViewInit, Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { AllTripResult } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';

@Component({
    selector: 'app-loadflow-trip-results',
    templateUrl: './loadflow-trip-results.component.html',
    styleUrls: ['./loadflow-trip-results.component.css']
})
export class LoadflowTripResultsComponent implements OnInit, OnDestroy, AfterViewInit {

    subs1: Subscription
    constructor(private dataService: LoadflowDataService) { 
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
