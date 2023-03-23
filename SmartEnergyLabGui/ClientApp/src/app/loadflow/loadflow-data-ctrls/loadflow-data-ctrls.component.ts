import { AfterViewInit, Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { CtrlWrapper, LoadflowCtrlType } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';

@Component({
    selector: 'app-loadflow-data-ctrls',
    templateUrl: './loadflow-data-ctrls.component.html',
    styleUrls: ['./loadflow-data-ctrls.component.css']
})
export class LoadflowDataCtrlsComponent implements OnInit, OnDestroy, AfterViewInit {

    private subs1:Subscription
    private subs2:Subscription
    constructor(private dataService: LoadflowDataService) {
        this.sort = null
        this.ctrls = this.createDataSource(dataService.networkData.ctrls);        
        this.displayedColumns = ['code','node1','node2','type','minCtrl','maxCtrl','cost','setPoint']
        this.subs1 = dataService.NetworkDataLoaded.subscribe( (results) => {
            this.ctrls = this.createDataSource(results.ctrls)
        })
        this.subs2 = dataService.ResultsLoaded.subscribe( (results) => {
            this.ctrls = this.createDataSource(results.ctrls)
        })
    }
    ngAfterViewInit(): void {
        this.ctrls.sort = this.sort
    }
    ngOnDestroy(): void {
        this.subs1.unsubscribe()
        this.subs2.unsubscribe()
    }

    ngOnInit(): void {

    }

    getTypeStr(type: LoadflowCtrlType) {
        return LoadflowCtrlType[type];
    }

    @ViewChild(MatSort) sort: MatSort | null;
    ctrls: MatTableDataSource<CtrlWrapper>
    displayedColumns: string[]

    getCtrlId(index: number, item: CtrlWrapper) {
        return item.obj.id;
    }

    private createDataSource(items: CtrlWrapper[]) : MatTableDataSource<CtrlWrapper> {
        let ds = new MatTableDataSource(items);
        ds.sortingDataAccessor = this.sortDataAccessor
        ds.sort = this.sort
        return ds
    }

    sortDataAccessor(data:CtrlWrapper, headerId: string) : number | string {
        if ( headerId == 'code') {
            return data.obj.code;
        } else if ( headerId == 'node1') {
            return data.branch.obj.node1Code
        } else if ( headerId == 'node2') {
            return data.branch.obj.node2Code
        } else if ( headerId == 'type') {
            return data.obj.type
        } else if ( headerId == 'minCtrl') {
            return data.obj.minCtrl
        } else if ( headerId == 'maxCtrl') {
            return data.obj.maxCtrl
        } else if ( headerId == 'cost') {
            return data.obj.cost
        } else if ( headerId == 'setPoint') {
            return data.setPoint ? data.setPoint : 0
        } else {
            return "";
        }
    }
}