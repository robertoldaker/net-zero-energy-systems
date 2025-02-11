import { Component } from '@angular/core';
import { Branch, BranchType } from '../../../data/app.data';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { ColumnDataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../../../datasets/data-table-base/data-table-base.component';

@Component({
  selector: 'app-loadflow-data-branches',
  templateUrl: './loadflow-data-branches.component.html',
  styleUrls: ['../loadflow-data-common.css','../../../datasets/data-table-base/data-table-base.component.css','./loadflow-data-branches.component.css']
})
export class LoadflowDataBranchesComponent extends DataTableBaseComponent<Branch> {

    constructor(private dataService: LoadflowDataService, private dialogService: DialogService) {
        super()
        this.dataFilter.sort = { active: 'node1Code', direction: 'asc'};
        this.typeDataFilter = new ColumnDataFilter(this,"typeStr")
        this.dataFilter.columnFilterMap.set(this.typeDataFilter.columnName, this.typeDataFilter)

        this.createDataSource(dataService.dataset,dataService.networkData.branches)
        this.displayedColumns = ['buttons','code','node1Code','node2Code','typeStr','x','cap','freePower','powerFlow','km','mwkm','loss']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(dataService.dataset,results.branches)
        }))
        this.addSub(dataService.ResultsLoaded.subscribe( (results) => {
            this.createDataSource(dataService.dataset,results.branches)
        }))
    }

    typeName: string = "Branch"
    typeDataFilter: ColumnDataFilter

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
