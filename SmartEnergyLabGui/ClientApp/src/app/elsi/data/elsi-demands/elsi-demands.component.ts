import { Component, ViewChild } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { DatasetData, ElsiPeakDemand, ElsiScenario } from 'src/app/data/app.data';
import { DataFilter } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ElsiDataService } from '../../elsi-data.service';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';
import { DataTableBaseComponent } from 'src/app/datasets/data-table-base/data-table-base.component';

@Component({
    selector: 'app-elsi-demands',
    templateUrl: './elsi-demands.component.html',
    styleUrls: ['./elsi-demands.component.css']
})
export class ElsiDemandsComponent extends DataTableBaseComponent<ElsiPeakDemand> {

    constructor(private service: ElsiDataService) {
        super()
        this.dataFilter.sort = { active: 'mainZoneStr', direction: 'asc'};
        this.displayedColumns = ['mainZoneStr', 'profileStr', 'communityRenewables', 'twoDegrees', 'steadyProgression', 'consumerEvolution','dummy']
        if (this.service.datasetInfo) {
            this.createDataSource(this.service.dataset,this.service.datasetInfo.peakDemandInfo)
        } 
        this.addSub(this.service.DatasetInfoChange.subscribe((ds) => {
            this.createDataSource(this.service.dataset,ds.peakDemandInfo)
        }))
    }
}

