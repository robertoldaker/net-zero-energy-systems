import { AfterViewInit, Component, ElementRef, HostListener, Input, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { AllTripResult, CtrlResult } from '../../data/app.data';
import { LoadflowSplitService } from '../loadflow-split.service';

@Component({
    selector: 'app-loadflow-trip-table',
    templateUrl: './loadflow-trip-table.component.html',
    styleUrls: ['./loadflow-trip-table.component.css']
})
export class LoadflowTripTableComponent implements OnInit, AfterViewInit, OnDestroy {

    subs1: Subscription
    constructor(private splitService: LoadflowSplitService) {
        this.sort = null
        this.ctrls=[]
        this.parentWidth = 'calc(100vw - 495px)';
        this.displayedColumns = ['surplus', 'capacity', 'trip', 'limCct']
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

    getTripName(index: number, item: AllTripResult) {
        return item.trip.text
    }

    displayedColumns: string[]
    ctrls: CtrlResult[]
    parentWidth: string

    @Input()
    trips: MatTableDataSource<AllTripResult>

    @ViewChild(MatSort) 
    sort: MatSort | null

}
