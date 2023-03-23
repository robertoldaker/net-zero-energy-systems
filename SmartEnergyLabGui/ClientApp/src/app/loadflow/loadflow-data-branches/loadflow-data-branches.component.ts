import { AfterViewInit, Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { BranchWrapper } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';

@Component({
  selector: 'app-loadflow-data-branches',
  templateUrl: './loadflow-data-branches.component.html',
  styleUrls: ['./loadflow-data-branches.component.css']
})
export class LoadflowDataBranchesComponent implements OnInit, OnDestroy, AfterViewInit {

    private subs1:Subscription
    private subs2:Subscription
    constructor(private dataService: LoadflowDataService) {
        this.sort = null;
        this.branches = this.createDataSource(dataService.networkData.branches)
        this.displayedColumns = ['code','node1','node2','region','x','cap','linkType','freePower','powerFlow']
        this.subs1 = dataService.NetworkDataLoaded.subscribe( (results) => {
            this.branches = this.createDataSource(results.branches)
        })
        this.subs2 = dataService.ResultsLoaded.subscribe( (results) => {
            this.branches = this.createDataSource(results.branches)
        })
    }

    private createDataSource(items: BranchWrapper[]): MatTableDataSource<BranchWrapper> {
        let branches = new MatTableDataSource(items)
        branches.sortingDataAccessor = this.sortDataAccessor
        branches.sort = this.sort
        return branches
    }

    ngAfterViewInit(): void {
        if ( this.branches ) {
            this.branches.sort = this.sort;
        }        
    }
    ngOnDestroy(): void {
        this.subs1.unsubscribe()
        this.subs2.unsubscribe()
    }

    ngOnInit(): void {

    }

    branches: MatTableDataSource<BranchWrapper>
    displayedColumns: string[]

    getBranchId(index: number, item: BranchWrapper) {
        return item.obj.id;
    }

    @ViewChild(MatSort) sort: MatSort | null;

    sortDataAccessor(data:BranchWrapper, headerId: string) : number | string {
        if ( headerId == 'code') {
            return data.obj.code
        } else if ( headerId == 'node1') {
            return data.obj.node1Code
        } else if ( headerId == 'node2') {
            return data.obj.node2Code
        } else if ( headerId == 'region' ) {
            return data.obj.region
        } else if ( headerId == 'x') {
            return data.obj.x
        } else if ( headerId == 'cap') {
            return data.obj.cap
        } else if ( headerId == 'linkType') {
            return data.obj.linkType
        } else if ( headerId == 'freePower') {
            return data.freePower ? data.freePower : 0
        } else if ( headerId == 'powerFlow') {
            return data.powerFlow ? data.powerFlow : 0
        } else {
            return ''
        }
    }

}
