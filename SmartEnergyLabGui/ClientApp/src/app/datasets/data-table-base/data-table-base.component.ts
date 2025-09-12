import { Component, OnInit, ViewChild } from '@angular/core';
import { Sort, SortDirection } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Dataset, DatasetData, IId } from 'src/app/data/app.data';
import { DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { BoundCalcDataService } from 'src/app/boundcalc/boundcalc-data-service.service';

@Component({
    selector: 'app-data-table-base',
    template: '<p>??</p>',
    styleUrls: ['./data-table-base.component.css']
})
export class DataTableBaseComponent<T extends IId> extends DialogBase  {

    constructor() {
        super()
        this.dataFilter = new DataFilter(20)
    }

    protected createDataSource(dataset?:Dataset, datasetData?: DatasetData<T>) {

        if ( datasetData && dataset ) {
            this.dataset = dataset
            this.datasetData = datasetData
            // only do this if was a change in dataset not a refresh
            if ( dataset.id != this.lastDatasetId ) {
                this.dataFilter.reset()
                this.tablePaginator?.firstPage()
                this.lastDatasetId = this.dataset.id
            }
        }
        if ( this.datasetData && this.dataset) {
            let cellData = this.dataFilter.GetCellDataObjects(this.dataset,this.datasetData,(item)=>item.id.toString())
            this.data = new MatTableDataSource(cellData)
            // this generates distinct values for column data filters
            for (let k of this.dataFilter.columnFilterMap.keys()) {
                let colFilter = this.dataFilter.columnFilterMap.get(k)
                if (colFilter) {
                    colFilter.genValues(this.datasetData.data)
                }
            }
        }
    }

    datasetData?: DatasetData<T>
    dataset?: Dataset
    dataFilter: DataFilter
    data: MatTableDataSource<ICellEditorDataDict> = new MatTableDataSource()
    displayedColumns: string[] = []
    lastDatasetId: number = 0

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

    filterTable(e?: DataFilter) {
        this.createDataSource()
    }

    newFilterTable() {
        this.tablePaginator?.firstPage()
        this.createDataSource()
    }

    get sortColumn(): string  {
        return this.dataFilter.sort ? this.dataFilter.sort.active : ''
    }

    get sortDirection(): SortDirection  {
        return this.dataFilter.sort ? this.dataFilter.sort.direction : ''
    }
}
