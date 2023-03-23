import { AfterViewInit, Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTable, MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { NodeWrapper } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';

@Component({
    selector: 'app-loadflow-data-nodes',
    templateUrl: './loadflow-data-nodes.component.html',
    styleUrls: ['./loadflow-data-nodes.component.css']
})
export class LoadflowDataNodesComponent implements OnInit, OnDestroy, AfterViewInit {

    private subs1:Subscription
    private subs2: Subscription
    constructor(private dataService: LoadflowDataService) {
        this.table = null
        this.sort = null
        console.log('loadflow nodes')
        console.log(dataService.networkData.nodes.length);
        this.nodes =  this.createDataSource(dataService.networkData.nodes);
        this.displayedColumns = ['code','zone','demand','generation','ext','mismatch']
        this.subs1 = dataService.NetworkDataLoaded.subscribe( (results) => {
            this.nodes = this.createDataSource(results.nodes);
        })
        this.subs2 = dataService.ResultsLoaded.subscribe( (results) => {
            this.nodes = this.createDataSource(results.nodes);
        })
    }

    private createDataSource(items:NodeWrapper[]):MatTableDataSource<NodeWrapper> {
        let nodes = new MatTableDataSource(items)
        nodes.sortingDataAccessor =this.sortDataAccessor
        if ( this.sort ) {
            nodes.sort = this.sort
        }
        return nodes
    }

    ngAfterViewInit(): void {
        if ( this.nodes ) {
            this.nodes.sort = this.sort;
        }
    }

    @ViewChild(MatSort) sort: MatSort | null;
    
    ngOnDestroy(): void {
        this.subs1.unsubscribe()
        this.subs2.unsubscribe()
    }

    ngOnInit(): void {

    }

    nodes: MatTableDataSource<NodeWrapper>
    displayedColumns: string[]
    @ViewChild(MatTable) table: MatTable<NodeWrapper> | null

    getNodeId(index: number, item: NodeWrapper) {
        return item.obj.id;
    }

    sortDataAccessor(data:NodeWrapper, headerId: string) : number | string {
        if ( headerId == 'code') {
            return data.obj.code;
        } else if ( headerId == 'zone') {
            return data.obj.zone.code
        } else if ( headerId == 'demand') {
            return data.obj.demand
        } else if ( headerId == 'generation') {
            return data.obj.generation
        } else if ( headerId == 'ext') {
            return data.obj.ext.toString()
        } else if ( headerId == 'mismatch') {
            return data.mismatch ? data.mismatch : 0;
        } else {
            return "";
        }
    }

}
