import { Component } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Branch, DatasetData } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { CellEditorData, DataFilter, ICellEditorDataDict } from 'src/app/utils/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
  selector: 'app-loadflow-data-branches',
  templateUrl: './loadflow-data-branches.component.html',
  styleUrls: ['./loadflow-data-branches.component.css']
})
export class LoadflowDataBranchesComponent extends ComponentBase {

    constructor(private dataService: LoadflowDataService) {
        super()
        this.createDataSource(dataService.networkData.branches)
        this.displayedColumns = ['code','node1Code','node2Code','region','x','cap','linkType','freePower','powerFlow']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(results.branches)
        }))
        this.addSub(dataService.ResultsLoaded.subscribe( (results) => {
            this.createDataSource(results.branches)
        }))
    }

    private createDataSource(datasetData?: DatasetData<Branch>): void {
        if ( datasetData ) {
            this.datasetData = datasetData
        }
        if ( this.datasetData) {
            let cellData = this.dataFilter.GetCellDataObjects(this.dataService.dataset,this.datasetData,(item)=>item.lineName)
            this.branches = new MatTableDataSource(cellData)            
        }
    }

    datasetData?: DatasetData<Branch>
    dataFilter: DataFilter = new DataFilter(20) 
    branches: MatTableDataSource<any> = new MatTableDataSource()
    displayedColumns: string[]

    getBranchId(index: number, item: Branch) {
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
