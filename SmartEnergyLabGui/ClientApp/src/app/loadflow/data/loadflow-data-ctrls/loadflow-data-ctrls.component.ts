import { Component } from '@angular/core';
import { Ctrl, LoadflowCtrlType } from '../../../data/app.data';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../data-table-base.component';

@Component({
    selector: 'app-loadflow-data-ctrls',
    templateUrl: './loadflow-data-ctrls.component.html',
    styleUrls: ['../loadflow-data-common.css','./loadflow-data-ctrls.component.css']
})
export class LoadflowDataCtrlsComponent extends DataTableBaseComponent<Ctrl> {

    constructor(
            dataService: LoadflowDataService,
            private dialogService: DialogService
        ) {
        super(dataService)
        this.dataFilter.sort = { active: 'code', direction: 'asc'};
        this.createDataSource(dataService.networkData.ctrls);        
        this.displayedColumns = ['buttons','code','node1Code','node2Code','type','minCtrl','maxCtrl','cost','setPoint']
        this.addSub(dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(results.ctrls)
        }))
        this.addSub(dataService.ResultsLoaded.subscribe( (results) => {
            this.createDataSource(results.ctrls)
        }))
    }

    getTypeStr(type: LoadflowCtrlType) {
        return LoadflowCtrlType[type];
    }

    typeName: string = "Ctrl"

    edit( e: ICellEditorDataDict) {
        let branchId = e._data.branchId
        let editorData = this.dataService.getBranchEditorData(branchId)
        this.dialogService.showLoadflowBranchDialog(editorData);
    }

}