import { Component, ViewChild } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Node, DatasetData } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { CellEditorData, DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { MatPaginator } from '@angular/material/paginator';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';
import { DatasetsService } from 'src/app/datasets/datasets.service';

@Component({
    selector: 'app-loadflow-data-nodes',
    templateUrl: './loadflow-data-nodes.component.html',
    styleUrls: ['./loadflow-data-nodes.component.css']
})
export class LoadflowDataNodesComponent extends ComponentBase {

    constructor(
        private dataService: LoadflowDataService, 
        private dialogService: DialogService,
        public datasetsService: DatasetsService ) {
        super();
        this.dataFilter = new DataFilter(20, { active: 'code', direction: 'asc'})
        this.createDataSource(this.dataService.networkData.nodes);
        this.displayedColumns = ['buttons','code','voltage','zoneName','demand','generation','ext','mismatch']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(results.nodes);
        }))
        this.addSub( dataService.ResultsLoaded.subscribe( (results) => {
            this.createDataSource(results.nodes);
        }))
    }

    private createDataSource(datasetData?: DatasetData<Node>) {

        if ( datasetData ) {
            this.datasetData = datasetData;
        }
        if ( this.datasetData ) {
            let cellData = this.dataFilter.GetCellDataObjects(this.dataService.dataset,this.datasetData,(item)=>item.id.toString())
            this.nodes = new MatTableDataSource(cellData)        
        } 
    }
    
    datasetData?: DatasetData<Node>
    dataFilter: DataFilter
    nodes: MatTableDataSource<any> = new MatTableDataSource()
    displayedColumns: string[]

    @ViewChild(TablePaginatorComponent)
    tablePaginator: TablePaginatorComponent | undefined

    getNodeId(index: number, item: Node) {
        return item.id;
    }

    sortTable(e:Sort) {
        this.dataFilter.sort = e
        this.dataFilter.skip = 0
        this.createDataSource()
        this.tablePaginator?.firstPage()
    }

    filterTable(e: DataFilter) {
        this.createDataSource()
    }

    edit( e: ICellEditorDataDict) {
        this.dialogService.showLoadflowNodeDialog(e);
    }

    add() {
        this.dialogService.showLoadflowNodeDialog();
    }

}
