import { Component } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Zone, DatasetData } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { DataFilter } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-loadflow-data-zones',
    templateUrl: './loadflow-data-zones.component.html',
    styleUrls: ['./loadflow-data-zones.component.css']
})
export class LoadflowDataZonesComponent extends ComponentBase {

    constructor(private dataService: LoadflowDataService) {
        super();
        this.createDataSource(this.dataService.networkData.zones);
        this.displayedColumns = ['code']
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
            this.nodes = new MatTableDataSource(cellData)        
        } 
    }
    
    datasetData?: DatasetData<Zone>
    dataFilter: DataFilter = new DataFilter(20) 
    nodes: MatTableDataSource<any> = new MatTableDataSource()
    displayedColumns: string[]

    getNodeId(index: number, item: Zone) {
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
