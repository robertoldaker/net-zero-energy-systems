import { Component, ViewChild } from '@angular/core';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Ctrl, DatasetData, LoadflowCtrlType } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { CellEditorData, DataFilter, ICellEditorDataDict } from 'src/app/utils/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-loadflow-data-ctrls',
    templateUrl: './loadflow-data-ctrls.component.html',
    styleUrls: ['./loadflow-data-ctrls.component.css']
})
export class LoadflowDataCtrlsComponent extends ComponentBase {

    constructor(private dataService: LoadflowDataService) {
        super()
        this.createDataSource(dataService.networkData.ctrls);        
        this.displayedColumns = ['code','node1Code','node2Code','type','minCtrl','maxCtrl','cost','setPoint']
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

    datasetData?: DatasetData<Ctrl>
    dataFilter: DataFilter = new DataFilter(20) 
    ctrls: MatTableDataSource<any> = new MatTableDataSource()
    displayedColumns: string[]

    getCtrlId(index: number, item: Ctrl) {
        return item.id;
    }

    private createDataSource(datasetData?: DatasetData<Ctrl>):void {
        if (datasetData) {
            this.datasetData = datasetData;
        }
        if ( this.datasetData) {
            let cellData = this.dataFilter.GetCellDataObjects(this.dataService.dataset, this.datasetData,(item)=>`${item.node1Code}-${item.node2Code}:${item.code}`)
            this.ctrls = new MatTableDataSource(cellData)    
        }
    }

    sortTable(e:Sort) {
        this.dataFilter.sort = e
        this.createDataSource()
    }

    filterTable(e: DataFilter) {
        this.createDataSource()
    }
}