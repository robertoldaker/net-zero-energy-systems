import { Component, ViewChild } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Zone, DatasetData, Boundary } from '../../../data/app.data';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
    selector: 'app-loadflow-data-boundaries',
    templateUrl: './loadflow-data-boundaries.component.html',
    styleUrls: ['../loadflow-data-common.css','./loadflow-data-boundaries.component.css']
})
export class LoadflowDataBoundariesComponent extends ComponentBase {

    constructor(private dataService: LoadflowDataService, private dialogService: DialogService) {
        super();
        this.createDataSource(this.dataService.networkData.boundaries);
        this.displayedColumns = ['buttons','code','zone']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(results.boundaries);
        }))
    }

    private createDataSource(datasetData?: DatasetData<Boundary>) {

        if ( datasetData ) {
            this.datasetData = datasetData;
        }
        if ( this.datasetData ) {
            let cellData = this.dataFilter.GetCellDataObjects(this.dataService.dataset,this.datasetData,(item)=>item.code)
            this.boundaries = new MatTableDataSource(cellData)        
        } 
    }
    
    @ViewChild(TablePaginatorComponent)
    tablePaginator: TablePaginatorComponent | undefined    
    datasetData?: DatasetData<Boundary>
    dataFilter: DataFilter = new DataFilter(20, { active: 'code', direction: 'asc'}) 
    boundaries: MatTableDataSource<any> = new MatTableDataSource()
    displayedColumns: string[]
    typeName: string = 'Boundary'

    getId(index: number, item: Boundary) {
        return item.id;
    }

    sortTable(e:Sort) {
        this.dataFilter.sort = e
        this.createDataSource()
        this.tablePaginator?.firstPage()
    }

    filterTable(e: DataFilter) {
        this.createDataSource()
    }

    edit( e: ICellEditorDataDict) {
        this.dialogService.showLoadflowBoundaryDialog(e);
    }

    add() {
        this.dialogService.showLoadflowBoundaryDialog();
    }

}
