import { Component} from '@angular/core';
import { Node} from 'src/app/data/app.data';
import { LoadflowDataService } from 'src/app/loadflow/loadflow-data-service.service';
import { ColumnDataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../../../datasets/data-table-base/data-table-base.component';

@Component({
    selector: 'app-loadflow-data-nodes',
    templateUrl: './loadflow-data-nodes.component.html',
    styleUrls: ['../loadflow-data-common.css','../../../datasets/data-table-base/data-table-base.component.css','./loadflow-data-nodes.component.css']
})
export class LoadflowDataNodesComponent extends DataTableBaseComponent<Node> {

    constructor(
        private dataService: LoadflowDataService, 
        private dialogService: DialogService,
     ) {
        super();
        this.dataFilter.sort = { active: 'code', direction: 'asc'};
        this.voltageDataFilter = new ColumnDataFilter(this,"voltage")
        this.dataFilter.columnFilterMap.set(this.voltageDataFilter.columnName, this.voltageDataFilter)
        this.zoneDataFilter = new ColumnDataFilter(this,"zoneName")
        this.dataFilter.columnFilterMap.set(this.zoneDataFilter.columnName, this.zoneDataFilter)

        this.createDataSource(this.dataService.dataset,dataService.networkData.nodes);
        this.displayedColumns = ['buttons','code','voltage','zoneName','demand','generation','ext','mismatch']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(this.dataService.dataset,results.nodes);
        }))
        this.addSub( dataService.ResultsLoaded.subscribe( (results) => {
            this.createDataSource(this.dataService.dataset,results.nodes);
        }))

    }

    edit( e: ICellEditorDataDict) {
        this.dialogService.showLoadflowNodeDialog(e);
    }

    add() {
        this.dialogService.showLoadflowNodeDialog();
    }

    voltageDataFilter: ColumnDataFilter 
    zoneDataFilter: ColumnDataFilter 

}


