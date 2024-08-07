import { Component, ViewChild } from '@angular/core';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Ctrl, DatasetData, GridSubstationLocation, GridSubstationLocationSource, IId, LoadflowCtrlType } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { CellEditorData, DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
    selector: 'app-loadflow-data-locations',
    templateUrl: './loadflow-data-locations.component.html',
    styleUrls: ['../loadflow-data-common.css','./loadflow-data-locations.component.css']
})
export class LoadflowDataLocationsComponent extends ComponentBase {

    constructor(
            private dataService: LoadflowDataService,
            private dialogService: DialogService
        ) {
        super()
        this.createDataSource(dataService.networkData.locations);        
        this.displayedColumns = ['buttons','reference','name','latitude','longitude','source']
        this.addSub(dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(results.locations)
        }))
    }

    getTypeStr(type: LoadflowCtrlType) {
        return LoadflowCtrlType[type];
    }

    @ViewChild(TablePaginatorComponent)
    tablePaginator: TablePaginatorComponent | undefined    
    datasetData?: DatasetData<GridSubstationLocation>
    dataFilter: DataFilter = new DataFilter(20, { active: 'code', direction: 'asc'}) 
    data: MatTableDataSource<any> = new MatTableDataSource()
    displayedColumns: string[]
    typeName: string = "GridSubstationLocation"

    getDataId(index: number, item: IId) {
        return item.id;
    }

    private createDataSource(datasetData?: DatasetData<GridSubstationLocation>):void {
        if (datasetData) {
            this.datasetData = datasetData;
        }
        if ( this.datasetData) {
            let cellData = this.dataFilter.GetCellDataObjects(this.dataService.dataset, this.datasetData,(item)=>item.id.toString())
            this.data = new MatTableDataSource(cellData)    
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
        this.dialogService.showLoadflowLocationDialog(e);
    }

    add() {
        this.dialogService.showLoadflowLocationDialog();
    }

    getSource(src: GridSubstationLocationSource) {
        return GridSubstationLocationSource[src]
    }
}
