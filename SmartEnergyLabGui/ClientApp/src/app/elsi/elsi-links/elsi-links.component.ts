import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { DatasetData, ElsiLink} from 'src/app/data/app.data';
import { CellEditorData, DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { ElsiDataService } from '../elsi-data.service';
import { ElsiGenCapacityTable } from '../elsi-gen-capacities/elsi-gen-capacities.component';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';

@Component({
    selector: 'app-elsi-links',
    templateUrl: './elsi-links.component.html',
    styleUrls: ['./elsi-links.component.css']
})
export class ElsiLinksComponent extends ComponentBase {

    constructor(public service: ElsiDataService) {
        super()
        this.sort = null
        this.displayedColumns = ['name','fromZoneStr','toZoneStr','capacity','revCap','loss','market','itf','itt','btf','btt']
        if ( this.service.datasetInfo) {
            this.createDataSource(this.service.datasetInfo.linkInfo)
        } 
        this.addSub( this.service.DatasetInfoChange.subscribe( (ds) => {
            this.createDataSource(ds.linkInfo)
        }))
    }

    private createDataSource(datasetData?: DatasetData<ElsiLink>) {

        if ( datasetData ) {
            this.datasetData = datasetData
        }
        if ( this.datasetData) {
            let cellData = this.dataFilter.GetCellDataObjects<ElsiLink>(this.service.dataset,this.datasetData,(item)=>item.name)
            this.tableData = new MatTableDataSource(cellData)    
        }
    }
    

    @ViewChild(TablePaginatorComponent)
    tablePaginator: TablePaginatorComponent | undefined    
    dataFilter: DataFilter = new DataFilter(20)
    datasetData?: DatasetData<ElsiLink>
    displayedColumns: string[]
    tableData: MatTableDataSource<any> = new MatTableDataSource()
    @ViewChild(MatSort) sort: MatSort | null;


    getId(index: number, item: ElsiLink) {
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
