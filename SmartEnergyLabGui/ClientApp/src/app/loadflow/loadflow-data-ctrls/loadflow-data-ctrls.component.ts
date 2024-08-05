import { Component, ViewChild } from '@angular/core';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Ctrl, DatasetData, LoadflowCtrlType } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { CellEditorData, DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
    selector: 'app-loadflow-data-ctrls',
    templateUrl: './loadflow-data-ctrls.component.html',
    styleUrls: ['../loadflow-data-common.css','./loadflow-data-ctrls.component.css']
})
export class LoadflowDataCtrlsComponent extends ComponentBase {

    constructor(
            private dataService: LoadflowDataService,
            private dialogService: DialogService
        ) {
        super()
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

    @ViewChild(TablePaginatorComponent)
    tablePaginator: TablePaginatorComponent | undefined    
    datasetData?: DatasetData<Ctrl>
    dataFilter: DataFilter = new DataFilter(20, { active: 'code', direction: 'asc'}) 
    ctrls: MatTableDataSource<any> = new MatTableDataSource()
    displayedColumns: string[]
    typeName: string = "Ctrl"

    getCtrlId(index: number, item: Ctrl) {
        return item.id;
    }

    private createDataSource(datasetData?: DatasetData<Ctrl>):void {
        if (datasetData) {
            this.datasetData = datasetData;
        }
        if ( this.datasetData) {
            let cellData = this.dataFilter.GetCellDataObjects(this.dataService.dataset, this.datasetData,(item)=>item.id.toString())
            this.ctrls = new MatTableDataSource(cellData)    
        }
    }

    sortTable(e:Sort) {
        this.dataFilter.sort = e
        this.createDataSource()
        this.tablePaginator?.firstPage()
    }

    filterTable(e: DataFilter) {
        this.createDataSource()
    }

    edit( e: ICellEditorDataDict) {
        this.dialogService.showLoadflowCtrlDialog(e);
    }

    add() {
        this.dialogService.showLoadflowCtrlDialog();
    }
}