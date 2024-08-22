import { Component } from '@angular/core';
import { Branch } from '../../../data/app.data';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../data-table-base.component';

@Component({
  selector: 'app-loadflow-data-branches',
  templateUrl: './loadflow-data-branches.component.html',
  styleUrls: ['../loadflow-data-common.css','./loadflow-data-branches.component.css']
})
export class LoadflowDataBranchesComponent extends DataTableBaseComponent<Branch> {

    constructor(dataService: LoadflowDataService, private dialogService: DialogService) {
        super(dataService)
        this.dataFilter.sort = { active: 'node1Code', direction: 'asc'};
        this.createDataSource(dataService.networkData.branches)
        this.displayedColumns = ['buttons','code','node1Code','node2Code','x','cap','linkType','freePower','powerFlow']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(results.branches)
        }))
        this.addSub(dataService.ResultsLoaded.subscribe( (results) => {
            this.createDataSource(results.branches)
        }))
    }

    typeName: string = "Branch"

    edit( e: ICellEditorDataDict) {
        this.dialogService.showLoadflowBranchDialog(e);
    }

    add() {
        this.dialogService.showLoadflowBranchDialog();
    }


}
