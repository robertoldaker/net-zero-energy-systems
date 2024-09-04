import { Component } from '@angular/core';
import { GridSubstationLocation, GridSubstationLocationSource, LoadflowCtrlType } from 'src/app/data/app.data';
import { LoadflowDataService } from 'src/app/loadflow/loadflow-data-service.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../data-table-base.component';
import { IDeleteItem } from 'src/app/datasets/cell-buttons/cell-buttons.component';

@Component({
    selector: 'app-loadflow-data-locations',
    templateUrl: './loadflow-data-locations.component.html',
    styleUrls: ['../loadflow-data-common.css','./loadflow-data-locations.component.css']
})
export class LoadflowDataLocationsComponent extends DataTableBaseComponent<GridSubstationLocation> {

    constructor(
            dataService: LoadflowDataService,
            private dialogService: DialogService
        ) {
        super(dataService)
        this.dataFilter.sort = { active: 'reference', direction: 'asc'};
        this.createDataSource(dataService.networkData.locations);        
        this.displayedColumns = ['buttons','reference','name','latitude','longitude','source']
        this.addSub(dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(results.locations)
        }))
    }

    typeName: string = "GridSubstationLocation"

    getTypeStr(type: LoadflowCtrlType) {
        return LoadflowCtrlType[type];
    }

    getSource(src: GridSubstationLocationSource) {
        return GridSubstationLocationSource[src]
    }

    edit( e: ICellEditorDataDict) {
        this.dialogService.showLoadflowLocationDialog(e);
    }

    add() {
        this.dialogService.showLoadflowLocationDialog();
    }

}
