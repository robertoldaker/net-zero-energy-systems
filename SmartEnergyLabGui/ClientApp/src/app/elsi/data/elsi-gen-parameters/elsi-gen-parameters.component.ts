import { Component } from '@angular/core';
import { ElsiGenParameter} from 'src/app/data/app.data';
import { ElsiDataService } from '../../elsi-data.service';
import { DataTableBaseComponent } from 'src/app/datasets/data-table-base/data-table-base.component';

@Component({
    selector: 'app-elsi-gen-parameters',
    templateUrl: './elsi-gen-parameters.component.html',
    styleUrls: ['./elsi-gen-parameters.component.css']
})
export class ElsiGenParametersComponent extends DataTableBaseComponent<ElsiGenParameter> {

    constructor(public service: ElsiDataService) {
        super()
        this.dataFilter.sort = { active: 'typeStr', direction: 'asc'};
        this.displayedColumns = ['typeStr','efficiency','emissionsRate','forcedDays','plannedDays','maintenanceCost','fuelCost','warmStart','wearAndTearStart','endurance','dummy']
        if ( this.service.datasetInfo) {
            this.createDataSource(this.service.dataset,this.service.datasetInfo.genParameterInfo)
        } 
        this.addSub( this.service.DatasetInfoChange.subscribe( (ds) => {
            this.createDataSource(this.service.dataset,ds.genParameterInfo)
        }))
    }
}
