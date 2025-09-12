import { Component } from '@angular/core';
import { Ctrl, BoundCalcCtrlType, SetPointMode } from '../../../data/app.data';
import { BoundCalcDataService } from '../../boundcalc-data-service.service';
import { ColumnDataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../../../datasets/data-table-base/data-table-base.component';
import { NodeZoneNum } from './boundcalc-data-ctrls-node-zone/boundcalc-data-ctrls-node-zone.component';

@Component({
    selector: 'app-boundcalc-data-ctrls',
    templateUrl: './boundcalc-data-ctrls.component.html',
    styleUrls: ['../boundcalc-data-common.css','../../../datasets/data-table-base/data-table-base.component.css','./boundcalc-data-ctrls.component.css']
})
export class BoundCalcDataCtrlsComponent extends DataTableBaseComponent<Ctrl> {

    constructor(
            private dataService: BoundCalcDataService,
            private dialogService: DialogService
        ) {
        super()
        this.dataFilter.sort = { active: 'code', direction: 'asc'};
        this.typeDataFilter = new ColumnDataFilter(this,"type",undefined,BoundCalcCtrlType)
        this.dataFilter.columnFilterMap.set(this.typeDataFilter.columnName, this.typeDataFilter)

        this.createDataSource(this.dataService.dataset,dataService.networkData.ctrls);
        this.displayedColumns = ['buttons','code','nodeZone1','nodeZone2','type','minCtrl','maxCtrl','cost','setPoint']
        this.addSub(dataService.NetworkDataLoaded.subscribe( (results) => {
            this.setPointError = false
            this.createDataSource(this.dataService.dataset,results.ctrls)
        }))
        this.addSub(dataService.ResultsLoaded.subscribe( (results) => {
            this.setPointError = results.setPointError;
            this.createDataSource(this.dataService.dataset,results.ctrls)
        }))
        this.addSub(dataService.SetPointModeChanged.subscribe( ( result) => {
        }))
    }

    getTypeStr(type: BoundCalcCtrlType) {
        return BoundCalcCtrlType[type];
    }

    typeName: string = "Ctrl"
    typeDataFilter: ColumnDataFilter

    edit( e: ICellEditorDataDict) {
        let branchId = e._data.branchId
        if ( branchId!=0) {
            let editorData = this.dataService.getBranchEditorData(branchId)
            this.dialogService.showBoundCalcBranchDialog(editorData);
        } else {
            this.dialogService.showBoundCalcCtrlDialog(e);
        }
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

    addCtrl() {
        this.dialogService.showBoundCalcCtrlDialog()
    }

    get setPointMode():SetPointMode {
        return this.dataService.setPointMode
    }

    SetPointMode = SetPointMode
    NodeZoneNum = NodeZoneNum

}
