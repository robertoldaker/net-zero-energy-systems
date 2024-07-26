import { Component, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { DataFilter, ICellEditorDataDict } from '../../datasets/cell-editor/cell-editor.component';
import { DatasetsService } from '../datasets.service';

@Component({
    selector: 'app-table-paginator',
    templateUrl: './table-paginator.component.html',
    styleUrls: ['./table-paginator.component.css']
})
export class TablePaginatorComponent implements OnInit {

    constructor(public datasetsService: DatasetsService) { 

    }

    ngOnInit(): void {

    }


    @Input()
    dataFilter:DataFilter = new DataFilter(20) 

    @Input()
    typeName:string = "?"

    @Output()
    onFilter: EventEmitter<DataFilter> = new EventEmitter<DataFilter>()

    @Output()
    onAdd: EventEmitter<any> = new EventEmitter<any>()

    @ViewChild(MatPaginator) 
    paginator: MatPaginator | null = null

    get addTitle():string {
        return `Add a new ${this.typeName}`;
    }
 
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

    raiseAddEvent() {
        this.datasetsService.canAdd( ()=>{
            this.onAdd.emit()
        });
    } 

    firstPage() {
        this.paginator?.firstPage()
    }
    
}

