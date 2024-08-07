import { Component, ViewChild } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Zone, DatasetData } from 'src/app/data/app.data';
import { LoadflowDataService } from 'src/app/loadflow/loadflow-data-service.service';
import { DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
    selector: 'app-loadflow-data-zones',
    templateUrl: './loadflow-data-zones.component.html',
    styleUrls: ['../loadflow-data-common.css','./loadflow-data-zones.component.css']
})
export class LoadflowDataZonesComponent extends ComponentBase {

    constructor(private dataService: LoadflowDataService, private dialogService:DialogService) {
        super();
        this.createDataSource(this.dataService.networkData.zones);
        this.displayedColumns = ['buttons','code']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(results.zones);
        }))
        this.addSub( dataService.ResultsLoaded.subscribe( (results) => {
            //this.createDataSource(results.zones);
        }))
    }

    private createDataSource(datasetData?: DatasetData<Zone>) {

        if ( datasetData ) {
            this.datasetData = datasetData;
        }
        if ( this.datasetData ) {
            let cellData = this.dataFilter.GetCellDataObjects(this.dataService.dataset,this.datasetData,(item)=>item.code)
            this.zones = new MatTableDataSource(cellData)        
        } 
    }
    
    @ViewChild(TablePaginatorComponent)
    tablePaginator: TablePaginatorComponent | undefined    
    datasetData?: DatasetData<Zone>
    dataFilter: DataFilter = new DataFilter(20,{ active: 'code', direction: 'asc'}) 
    zones: MatTableDataSource<any> = new MatTableDataSource()
    displayedColumns: string[]

    getNodeId(index: number, item: Zone) {
        return item.id;
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
        this.dialogService.showLoadflowZoneDialog(e);
    }

    add() {
        this.dialogService.showLoadflowZoneDialog();
    }

}
