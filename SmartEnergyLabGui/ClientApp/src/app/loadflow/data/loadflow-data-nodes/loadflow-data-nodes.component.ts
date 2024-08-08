import { Component} from '@angular/core';
import { Node} from 'src/app/data/app.data';
import { LoadflowDataService } from 'src/app/loadflow/loadflow-data-service.service';
import { DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { MessageDialog } from 'src/app/dialogs/message-dialog/message-dialog.component';
import { DataTableBaseComponent } from 'src/app/loadflow/data/data-table-base.component';

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
        this.dataFilter.sort = { active: 'code', direction: 'asc'};
        this.createDataSource(dataService.networkData.nodes);
        this.displayedColumns = ['buttons','code','voltage','zoneName','demand','generation','ext','mismatch']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
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

    delete(e:any) {
        let element = e.element;
        let id = element._data.id
        let bs = this.dataService.networkData.branches.data.filter(m=>m.node1Id == id || m.node2Id== id)
        if ( bs.length>0 ) {
            this.dialogService.showMessageDialog(new MessageDialog(`Cannot delete node since it used by <b>${bs.length}</b> branches`))
            e.canDelete = false
        }
    }

}
