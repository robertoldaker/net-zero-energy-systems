import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Dataset, DatasetData, ElsiGenCapacity, ElsiProfile, ElsiScenario} from 'src/app/data/app.data';
import { CellEditorData, DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { ElsiDataService } from '../../elsi-data.service';
import { MATERIAL_SANITY_CHECKS } from '@angular/material/core';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';

@Component({
    selector: 'app-elsi-gen-capacities',
    templateUrl: './elsi-gen-capacities.component.html',
    styleUrls: ['./elsi-gen-capacities.component.css']
})

export class ElsiGenCapacitiesComponent extends ComponentBase {

    constructor(public service: ElsiDataService) {
        super()
        this.dataFilter.sort = { active: 'zoneStr', direction: 'asc'};
        this.displayedColumns = ['zoneStr','genTypeStr','profileStr','communityRenewables','twoDegrees','steadyProgression','consumerEvolution','dummy']
        if (this.service.datasetInfo) {
            this.createDataSource(this.service.datasetInfo.genCapacityInfo)
        } 
        this.addSub(this.service.DatasetInfoChange.subscribe((ds) => {
            this.createDataSource(ds.genCapacityInfo)
        }))



    }

    private createDataSource(datasetData?: DatasetData<ElsiGenCapacity>) {
        if ( datasetData ) {
            this.datasetData = datasetData
        }
        if ( this.datasetData) {
            let cellData = this.dataFilter.GetCellDataObjects<ElsiGenCapacity>(
                this.service.dataset,
                this.datasetData,
                (item)=>item.id.toString()
            )
            this.tableData = new MatTableDataSource(cellData)    
        }
        return 
    }

    @ViewChild(TablePaginatorComponent)
    tablePaginator: TablePaginatorComponent | undefined    
    dataFilter: DataFilter = new DataFilter(20)
    datasetData?: DatasetData<ElsiGenCapacity>
    displayedColumns: string[]
    tableData: MatTableDataSource<any> = new MatTableDataSource()

    getId(index: number, item: ElsiGenCapacity) {
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
