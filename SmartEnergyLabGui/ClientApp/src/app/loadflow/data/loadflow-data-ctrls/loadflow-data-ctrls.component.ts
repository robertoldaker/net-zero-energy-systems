import { Component } from '@angular/core';
import { Ctrl, LoadflowCtrlType, SetPointMode } from '../../../data/app.data';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { ColumnDataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../../../datasets/data-table-base/data-table-base.component';

@Component({
    selector: 'app-loadflow-data-ctrls',
    templateUrl: './loadflow-data-ctrls.component.html',
    styleUrls: ['../loadflow-data-common.css','../../../datasets/data-table-base/data-table-base.component.css','./loadflow-data-ctrls.component.css']
})
export class LoadflowDataCtrlsComponent extends DataTableBaseComponent<Ctrl> {

    constructor(
            private dataService: LoadflowDataService,
            private dialogService: DialogService
        ) {
        super()
        this.dataFilter.sort = { active: 'code', direction: 'asc'};
        this.typeDataFilter = new ColumnDataFilter(this,"type",undefined,LoadflowCtrlType)
        this.dataFilter.columnFilterMap.set(this.typeDataFilter.columnName, this.typeDataFilter)

        this.createDataSource(this.dataService.dataset,dataService.networkData.ctrls);        
        this.displayedColumns = ['buttons','code','node1Code','node2Code','type','minCtrl','maxCtrl','cost','setPoint']
        this.addSub(dataService.NetworkDataLoaded.subscribe( (results) => {
            this.setPointError = false
            this.createDataSource(this.dataService.dataset,results.ctrls)
        }))
        this.addSub(dataService.ResultsLoaded.subscribe( (results) => {
            this.setPointError = results.setPointError;
            this.createDataSource(this.dataService.dataset,results.ctrls)
        }))
        this.addSub(dataService.SetPointModeChanged.subscribe( ( result) => {
            console.log('setPointMode',result)
        }))
    }

    getTypeStr(type: LoadflowCtrlType) {
        return LoadflowCtrlType[type];
    }

    typeName: string = "Ctrl"
    typeDataFilter: ColumnDataFilter

    edit( e: ICellEditorDataDict) {
        let branchId = e._data.branchId
        let editorData = this.dataService.getBranchEditorData(branchId)
        this.dialogService.showLoadflowBranchDialog(editorData);
    }

    setPointError: boolean = false

    getOverallSetPointStyle(): any {
        if ( this.setPointMode == SetPointMode.Manual) {
            return {'font-style': 'normal'}
        } else {
            return this.setPointError ? {'color':'darkred'} : {'color': 'green'}
        }
    }

    getSetPointStyle(sp: number | undefined,min: number, max: number): any {
        if (sp!=undefined && (sp>max || sp<min)) {
            return {'color':'darkred'}
        } else {
            return {'color': 'green'};
        }
    }

    filterByCode(code: string) {
        //
        this.dataFilter.reset(true)
        this.dataFilter.searchStr = code
        this.createDataSource()
    }

    get setPointMode():SetPointMode {
        return this.dataService.setPointMode
    }

    SetPointMode = SetPointMode

}