import { Component, OnInit, ViewChild } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { DatasetData, IId } from 'src/app/data/app.data';
import { DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { LoadflowDataService } from 'src/app/loadflow/loadflow-data-service.service';

@Component({
    selector: 'app-data-table-base',
    template: '<p>??</p>',
    styleUrls: ['./data-table-base.component.css']
})
export class DataTableBaseComponent<T extends IId> extends DialogBase  {

    constructor(        
            protected dataService: LoadflowDataService
        ) {
        super()
        this.dataFilter = new DataFilter(20)        
    }

    protected createDataSource(datasetData?: DatasetData<T>) {

        if ( datasetData ) {
            this.datasetData = datasetData
            this.dataFilter.reset()
            this.tablePaginator?.firstPage()
        }
        if ( this.datasetData ) {
            let cellData = this.dataFilter.GetCellDataObjects(this.dataService.dataset,this.datasetData,(item)=>item.id.toString())
            this.data = new MatTableDataSource(cellData)        
        } 
    }
    
    datasetData?: DatasetData<T>
    dataFilter: DataFilter
    data: MatTableDataSource<ICellEditorDataDict> = new MatTableDataSource()
    displayedColumns: string[] = []

    @ViewChild(TablePaginatorComponent)
    tablePaginator: TablePaginatorComponent | undefined

    getDataId(index: number, item: ICellEditorDataDict) {
        return item.id;
    }

    sortTable(e:Sort) {
        this.dataFilter.sort = e
        this.dataFilter.skip = 0
        this.createDataSource()
        this.tablePaginator?.firstPage()
    }

    filterTable(e: DataFilter) {
        this.createDataSource()
    }

}
