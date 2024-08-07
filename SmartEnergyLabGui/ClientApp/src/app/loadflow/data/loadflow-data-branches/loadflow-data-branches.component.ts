import { Component, ViewChild } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Branch, DatasetData } from '../../../data/app.data';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { CellEditorData, DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
  selector: 'app-loadflow-data-branches',
  templateUrl: './loadflow-data-branches.component.html',
  styleUrls: ['../loadflow-data-common.css','./loadflow-data-branches.component.css']
})
export class LoadflowDataBranchesComponent extends ComponentBase {

    constructor(private dataService: LoadflowDataService, private dialogService: DialogService) {
        super()
        this.createDataSource(dataService.networkData.branches)
        this.displayedColumns = ['buttons','code','node1Code','node2Code','x','cap','linkType','freePower','powerFlow']
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
            let cellData = this.dataFilter.GetCellDataObjects(this.dataService.dataset,this.datasetData,(item)=>item.id.toString())
            this.branches = new MatTableDataSource(cellData)            
        }
    }

    @ViewChild(TablePaginatorComponent)
    tablePaginator: TablePaginatorComponent | undefined    
    datasetData?: DatasetData<Branch>
    dataFilter: DataFilter = new DataFilter(20, { active: 'code', direction: 'asc'}) 
    branches: MatTableDataSource<any> = new MatTableDataSource()
    displayedColumns: string[]
    typeName: string = "Branch"

    getBranchId(index: number, item: Branch) {
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
        this.dialogService.showLoadflowBranchDialog(e);
    }

    add() {
        this.dialogService.showLoadflowBranchDialog();
    }


}
