import { Component } from '@angular/core';
import { GridSubstationLocation, GridSubstationLocationSource, LoadflowCtrlType } from 'src/app/data/app.data';
import { LoadflowDataService } from 'src/app/loadflow/loadflow-data-service.service';
import { ColumnDataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../../../datasets/data-table-base/data-table-base.component';

@Component({
    selector: 'app-loadflow-data-locations',
    templateUrl: './loadflow-data-locations.component.html',
    styleUrls: ['../loadflow-data-common.css','../../../datasets/data-table-base/data-table-base.component.css','./loadflow-data-locations.component.css']
})
export class LoadflowDataLocationsComponent extends DataTableBaseComponent<GridSubstationLocation> {

    constructor(
            private dataService: LoadflowDataService,
            private dialogService: DialogService
        ) {
        super()
        this.dataFilter.sort = { active: 'reference', direction: 'asc'};

        this.sourceDataFilter = new ColumnDataFilter(this,"source",undefined,GridSubstationLocationSource)
        this.dataFilter.columnFilterMap.set(this.sourceDataFilter.columnName, this.sourceDataFilter)

        this.createDataSource(this.dataService.dataset,this.dataService.networkData.locations);        
        this.displayedColumns = ['buttons','reference','name','latitude','longitude','source']
        this.addSub(dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(this.dataService.dataset,results.locations)
        }))
    }

    typeName: string = "GridSubstationLocation"
    sourceDataFilter: ColumnDataFilter

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
