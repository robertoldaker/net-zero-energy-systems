import { Component, ViewChild } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Zone, DatasetData, Boundary } from '../../../data/app.data';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../data-table-base.component';

@Component({
    selector: 'app-loadflow-data-boundaries',
    templateUrl: './loadflow-data-boundaries.component.html',
    styleUrls: ['../loadflow-data-common.css','./loadflow-data-boundaries.component.css']
})
export class LoadflowDataBoundariesComponent extends DataTableBaseComponent<Boundary> {

    constructor(dataService: LoadflowDataService, private dialogService: DialogService) {
        super(dataService);
        this.dataFilter.sort = { active: 'code', direction: 'asc'};
        this.createDataSource(this.dataService.networkData.boundaries);
        this.displayedColumns = ['buttons','code','zone']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(results.boundaries);
        }))
    }
    
    typeName: string = 'Boundary'

    edit( e: ICellEditorDataDict) {
        this.dialogService.showLoadflowBoundaryDialog(e);
    }

    add() {
        this.dialogService.showLoadflowBoundaryDialog();
    }

}
