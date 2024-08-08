import { Component, ViewChild } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Zone, DatasetData } from 'src/app/data/app.data';
import { LoadflowDataService } from 'src/app/loadflow/loadflow-data-service.service';
import { DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../data-table-base.component';

@Component({
    selector: 'app-loadflow-data-zones',
    templateUrl: './loadflow-data-zones.component.html',
    styleUrls: ['../loadflow-data-common.css','./loadflow-data-zones.component.css']
})
export class LoadflowDataZonesComponent extends DataTableBaseComponent<Zone> {

    constructor(dataService: LoadflowDataService, private dialogService:DialogService) {
        super(dataService);
        this.dataFilter.sort = { active: 'code', direction: 'asc'};
        this.createDataSource(this.dataService.networkData.zones);
        this.displayedColumns = ['buttons','code']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(results.zones);
        }))
    }

    edit( e: ICellEditorDataDict) {
        this.dialogService.showLoadflowZoneDialog(e);
    }

    add() {
        this.dialogService.showLoadflowZoneDialog();
    }

}
