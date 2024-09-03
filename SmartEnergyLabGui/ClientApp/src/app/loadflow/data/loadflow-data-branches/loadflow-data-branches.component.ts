import { Component } from '@angular/core';
import { Branch, BranchType } from '../../../data/app.data';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
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
        this.displayedColumns = ['buttons','code','node1Code','node2Code','type','x','cap','freePower','powerFlow']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(results.branches)
        }))
        this.addSub(dataService.ResultsLoaded.subscribe( (results) => {
            this.createDataSource(results.branches)
        }))
    }

    typeName: string = "Branch"

    getTypeStr(type: BranchType) {
        return BranchType[type];
    }

    edit( e: ICellEditorDataDict) {
        let branchId = e._data.id
        let branchEditorData = this.dataService.getBranchEditorData(branchId)
        this.dialogService.showLoadflowBranchDialog(branchEditorData);
    }

    add() {
        this.dialogService.showLoadflowBranchDialog();
    }
}
