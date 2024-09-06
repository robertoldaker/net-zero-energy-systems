import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { DatasetData, ElsiGenParameter} from 'src/app/data/app.data';
import { CellEditorData, DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { ElsiDataService } from '../../elsi-data.service';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';

@Component({
    selector: 'app-elsi-gen-parameters',
    templateUrl: './elsi-gen-parameters.component.html',
    styleUrls: ['./elsi-gen-parameters.component.css']
})
export class ElsiGenParametersComponent extends ComponentBase {


    constructor(public service: ElsiDataService) {
        super()
        this.dataFilter.sort = { active: 'typeStr', direction: 'asc'};
        this.displayedColumns = ['typeStr','efficiency','emissionsRate','forcedDays','plannedDays','maintenanceCost','fuelCost','warmStart','wearAndTearStart','endurance','dummy']
        if ( this.service.datasetInfo) {
            this.createDataSource(this.service.datasetInfo.genParameterInfo)
        } 
        this.addSub( this.service.DatasetInfoChange.subscribe( (ds) => {
            this.createDataSource(ds.genParameterInfo)
        }))
    }

    private createDataSource(datasetData?: DatasetData<ElsiGenParameter>) {

        if ( datasetData ) {
            this.datasetData = datasetData;
        }
        if ( this.datasetData) {
            let cellData = this.dataFilter.GetCellDataObjects<ElsiGenParameter>(this.service.dataset, this.datasetData,(item)=>item.id.toString())
            this.tableData = new MatTableDataSource(cellData)    
        }
    }
    
    @ViewChild(TablePaginatorComponent)
    tablePaginator: TablePaginatorComponent | undefined    
    dataFilter: DataFilter = new DataFilter(20)
    datasetData?: DatasetData<ElsiGenParameter>
    tableData: MatTableDataSource<any> = new MatTableDataSource()
    displayedColumns: string[]

    getId(index: number, item: ElsiGenParameter) {
        return item.id
    }

    filterTable(e: DataFilter) {
        this.createDataSource()
    }

    sortTable(e:Sort) {
        this.dataFilter.sort = e
        this.createDataSource()
        this.tablePaginator?.firstPage()
    }

}
