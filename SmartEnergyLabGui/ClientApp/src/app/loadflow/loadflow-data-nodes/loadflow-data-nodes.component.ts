import { Component } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Node, DatasetData } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { CellEditorData, DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-loadflow-data-nodes',
    templateUrl: './loadflow-data-nodes.component.html',
    styleUrls: ['./loadflow-data-nodes.component.css']
})
export class LoadflowDataNodesComponent extends ComponentBase {

    constructor(private dataService: LoadflowDataService) {
        super();
        this.createDataSource(this.dataService.networkData.nodes);
        this.displayedColumns = ['code','zoneName','demand','generation','ext','mismatch']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(results.nodes);
        }))
        this.addSub( dataService.ResultsLoaded.subscribe( (results) => {
            console.log('results loaded')
            this.createDataSource(results.nodes);
        }))
    }

    private createDataSource(datasetData?: DatasetData<Node>) {

        if ( datasetData ) {
            this.datasetData = datasetData;
        }
        if ( this.datasetData ) {
            let cellData = this.dataFilter.GetCellDataObjects(this.dataService.dataset,this.datasetData,(item)=>item.code)
            this.nodes = new MatTableDataSource(cellData)        
        } 
    }
    
    datasetData?: DatasetData<Node>
    dataFilter: DataFilter = new DataFilter(20) 
    nodes: MatTableDataSource<any> = new MatTableDataSource()
    displayedColumns: string[]

    getNodeId(index: number, item: Node) {
        return item.id;
    }

    sortTable(e:Sort) {
        this.dataFilter.sort = e
        this.createDataSource()
    }

    filterTable(e: DataFilter) {
        this.createDataSource()
    }

}
