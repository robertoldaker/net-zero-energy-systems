import { Component} from '@angular/core';
import { Node} from 'src/app/data/app.data';
import { LoadflowDataService } from 'src/app/loadflow/loadflow-data-service.service';
import { DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { MessageDialog } from 'src/app/dialogs/message-dialog/message-dialog.component';
import { DataTableBaseComponent } from 'src/app/loadflow/data/data-table-base.component';
import { IDeleteItem } from 'src/app/datasets/cell-buttons/cell-buttons.component';

@Component({
    selector: 'app-loadflow-data-nodes',
    templateUrl: './loadflow-data-nodes.component.html',
    styleUrls: ['../loadflow-data-common.css','./loadflow-data-nodes.component.css']
})
export class LoadflowDataNodesComponent extends DataTableBaseComponent<Node> {

    constructor(
        dataService: LoadflowDataService, 
        private dialogService: DialogService,
     ) {
        super(dataService);
        console.log('nodes constructor')
        this.dataFilter.sort = { active: 'code', direction: 'asc'};
        this.createDataSource(dataService.networkData.nodes);
        this.displayedColumns = ['buttons','code','voltage','zoneName','demand','generation','ext','mismatch']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            console.log('NetworkDataLoaded')
            console.log(results.nodes.data.length)
            this.createDataSource(results.nodes);
        }))
        this.addSub( dataService.ResultsLoaded.subscribe( (results) => {
            this.createDataSource(results.nodes);
        }))
    }

    edit( e: ICellEditorDataDict) {
        this.dialogService.showLoadflowNodeDialog(e);
    }

    add() {
        this.dialogService.showLoadflowNodeDialog();
    }

}
