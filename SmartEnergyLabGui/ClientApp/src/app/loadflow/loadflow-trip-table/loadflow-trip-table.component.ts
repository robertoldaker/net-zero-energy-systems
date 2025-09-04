import { AfterViewInit, Component, ElementRef, HostListener, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { AllTripResult, BoundaryTrip, CtrlResult } from '../../data/app.data';
import { LoadflowSplitService } from '../loadflow-split.service';
import { LoadflowDataService } from '../loadflow-data-service.service';

@Component({
    selector: 'app-loadflow-trip-table',
    templateUrl: './loadflow-trip-table.component.html',
    styleUrls: ['./loadflow-trip-table.component.css']
})
export class LoadflowTripTableComponent implements OnInit, AfterViewInit {

    constructor(private dataService: LoadflowDataService) {
        this.sort = null
        this.parentWidth = 'calc(100vw - 495px)';
        this.displayedColumns = ['selected','capacity', 'surplus', 'trip', 'limCct','tripOutcome']
        this.trips = new MatTableDataSource();
    }
    ngAfterViewInit(): void {
        if ( this.sort ) {
            this.trips.sort = this.sort
        }
    }

    ngOnInit(): void {
    }

    getTripName(index: number, item: AllTripResult) {
        return (item.trip != null ) ? item.trip.text : "Intact"
    }

    tripSelected(tripResult: AllTripResult) {
        this.dataService.runBoundaryTrip(tripResult)
    }

    getSelectedStyle(trip: BoundaryTrip |  null) {
        let style = {}
        if ( trip?.text === this.dataService.boundaryTripResult?.trip?.text ) {
            style = {borderLeftColor: 'green'}
        }
        return style
    }

    getTripOutcomeStyle(tripOutcome: string): any {
        if (tripOutcome) {
            return { 'color': 'darkred' }
        } else {
            return {};
        }
    }

    getOverallTripOutcomeStyle(): any {
        let t = this.trips.data.find( m=>m.tripOutcome )
        if ( t ) {
            return { 'color': 'darkred' }
        } else {
            return {};
        }
    }

    displayedColumns: string[]
    parentWidth: string

    @Input()
    trips: MatTableDataSource<AllTripResult>

    @ViewChild(MatSort)
    sort: MatSort | null

}
