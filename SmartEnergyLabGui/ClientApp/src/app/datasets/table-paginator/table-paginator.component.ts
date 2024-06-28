import { Component, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { DataFilter } from '../../datasets/cell-editor/cell-editor.component';

@Component({
    selector: 'app-table-paginator',
    templateUrl: './table-paginator.component.html',
    styleUrls: ['./table-paginator.component.css']
})
export class TablePaginatorComponent implements OnInit {

    constructor() { 

    }

    ngOnInit(): void {

    }


    @Input()
    dataFilter:DataFilter = new DataFilter(20) 

    @Output()
    onFilter: EventEmitter<DataFilter> = new EventEmitter<DataFilter>()

    @ViewChild(MatPaginator) 
    paginator: MatPaginator | null = null
 
    page(e: PageEvent) {
        this.dataFilter.skip = e.pageIndex*e.pageSize
        this.raiseFilterEvent()
    }

    searchStr = ''
    clear() {
        if ( this.paginator ) {
            this.dataFilter.searchStr  =''
            this.paginator.firstPage()
            this.raiseFilterEvent()
        }
    }

    filter(e: any) {
        if ( this.paginator ) {
            this.paginator.firstPage()
            this.raiseFilterEvent()
        }
    }

    raiseFilterEvent() {
        this.onFilter.emit(this.dataFilter)
    } 

    firstPage() {
        this.paginator?.firstPage()
    }
    
}

