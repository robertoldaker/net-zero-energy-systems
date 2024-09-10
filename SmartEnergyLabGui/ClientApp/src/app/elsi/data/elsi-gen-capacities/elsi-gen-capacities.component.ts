import { Component } from '@angular/core';
import { ElsiGenCapacity} from 'src/app/data/app.data';
import { ElsiDataService } from '../../elsi-data.service';
import { DataTableBaseComponent } from 'src/app/datasets/data-table-base/data-table-base.component';
import { ColumnDataFilter } from 'src/app/datasets/cell-editor/cell-editor.component';

@Component({
    selector: 'app-elsi-gen-capacities',
    templateUrl: './elsi-gen-capacities.component.html',
    styleUrls: ['./elsi-gen-capacities.component.css']
})

export class ElsiGenCapacitiesComponent extends DataTableBaseComponent<ElsiGenCapacity> {

    constructor(public service: ElsiDataService) {
        super()
        this.dataFilter.sort = { active: 'zoneStr', direction: 'asc'};

        // zone filter
        this.zoneDataFilter = new ColumnDataFilter(this,"zoneStr")
        this.dataFilter.columnFilterMap.set(this.zoneDataFilter.columnName, this.zoneDataFilter)
        // type filter
        this.typeDataFilter = new ColumnDataFilter(this,"genTypeStr")
        this.dataFilter.columnFilterMap.set(this.typeDataFilter.columnName, this.typeDataFilter)
        // profile filter
        this.profileDataFilter = new ColumnDataFilter(this,"profileStr")
        this.dataFilter.columnFilterMap.set(this.profileDataFilter.columnName, this.profileDataFilter)

        this.displayedColumns = ['zoneStr','genTypeStr','profileStr','communityRenewables','twoDegrees','steadyProgression','consumerEvolution','dummy']
        if (this.service.datasetInfo) {
            this.createDataSource(this.service.dataset, this.service.datasetInfo.genCapacityInfo)
        } 
        this.addSub(this.service.DatasetInfoChange.subscribe((ds) => {
            this.createDataSource(this.service.dataset,ds.genCapacityInfo)
        }))
    }
    zoneDataFilter: ColumnDataFilter
    typeDataFilter: ColumnDataFilter
    profileDataFilter: ColumnDataFilter
}
