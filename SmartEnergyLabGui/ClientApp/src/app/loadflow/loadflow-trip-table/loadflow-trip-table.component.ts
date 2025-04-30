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
export class LoadflowTripTableComponent implements OnInit, AfterViewInit, OnDestroy {

    subs1: Subscription
    constructor(private splitService: LoadflowSplitService, private dataService: LoadflowDataService) {
        this.sort = null
        this.ctrls=[]
        this.parentWidth = 'calc(100vw - 495px)';
        this.displayedColumns = ['selected','capacity', 'surplus', 'trip', 'limCct']
        this.trips = new MatTableDataSource();
        this.subs1 = splitService.SplitChange.subscribe( (splitData)=> {
            let clientWidth = splitData.left + 45
            this.parentWidth = `calc(100vw - ${clientWidth}px)`;
        })
    }
    ngOnDestroy(): void {
        this.subs1.unsubscribe()
    }
    ngAfterViewInit(): void {
        if ( this.sort ) {
            this.trips.sort = this.sort
        }
    }

    ngOnInit(): void {
        if ( this.trips.data.length>0) {
            this.ctrls = this.trips.data[0].ctrls
            this.ctrls.forEach((c)=>{
                this.displayedColumns.push(c.code)
            })
        }
    }

    getCtrls() {
        if ( this.trips.data.length>0) {
            return this.trips.data[0].ctrls
        } else {
            return []
        }
    }

    getTripName(index: number, item: AllTripResult) {
        return (item.trip != null ) ? item.trip.text : "Intact"
    }

    getSetPoint(item: AllTripResult, ctrlCode: string):string {
        if ( item.ctrls ) {
            let ctrl = item.ctrls.find(m=>m.code == ctrlCode);
            if ( ctrl && ctrl.setPoint) {
                return ctrl?.setPoint?.toFixed(2)
            } else {
                return "0.00"
            }    
        } else {
            return ""
        }
    }

    tripSelected(trip: BoundaryTrip | null) {
        this.dataService.runBoundaryTrip(trip)
    }

    getSelectedStyle(trip: BoundaryTrip |  null) {
        let style = {}
        if ( trip?.text === this.dataService.boundaryTrip?.text ) {
            style = {borderLeftColor: 'green'}
        }
        return style
    }

    displayedColumns: string[]
    ctrls: CtrlResult[]
    parentWidth: string

    @Input()
    trips: MatTableDataSource<AllTripResult>

    @ViewChild(MatSort) 
    sort: MatSort | null

}
