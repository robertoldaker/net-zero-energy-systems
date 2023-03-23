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
        if ( dataService.loadFlowResults ) {
            this.singleTrips = this.createDataSource(dataService.loadFlowResults.singleTrips)
            this.doubleTrips = this.createDataSource(dataService.loadFlowResults.doubleTrips)
        } else {
            this.singleTrips = this.createDataSource([])
            this.doubleTrips = this.createDataSource([])
        }
        this.subs1 = dataService.ResultsLoaded.subscribe( (results) => {
            if ( results.singleTrips ) {
                this.singleTrips = this.createDataSource(results.singleTrips)
            }
            if ( results.doubleTrips ) {
                this.doubleTrips = this.createDataSource(results.doubleTrips)
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

    singleTrips: MatTableDataSource<AllTripResult>
    doubleTrips: MatTableDataSource<AllTripResult>

}
