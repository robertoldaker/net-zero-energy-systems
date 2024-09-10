import { Component, ViewChild } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Zone, DatasetData } from 'src/app/data/app.data';
import { LoadflowDataService } from 'src/app/loadflow/loadflow-data-service.service';
import { DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../../../datasets/data-table-base/data-table-base.component';

@Component({
    selector: 'app-loadflow-data-zones',
    templateUrl: './loadflow-data-zones.component.html',
    styleUrls: ['../loadflow-data-common.css','../../../datasets/data-table-base/data-table-base.component.css','./loadflow-data-zones.component.css']
})
export class LoadflowDataZonesComponent extends DataTableBaseComponent<Zone> {

    constructor(private dataService: LoadflowDataService, private dialogService:DialogService) {
        super();
        this.dataFilter.sort = { active: 'code', direction: 'asc'};
        this.createDataSource(dataService.dataset,this.dataService.networkData.zones);
        this.displayedColumns = ['buttons','code']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(this.dataService.dataset,results.zones);
        }))
    }

    edit( e: ICellEditorDataDict) {
        this.dialogService.showLoadflowZoneDialog(e);
    }

    add() {
        this.dialogService.showLoadflowZoneDialog();
    }

}
