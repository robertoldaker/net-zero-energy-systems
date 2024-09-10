import { Component } from '@angular/core';
import { ElsiLink} from 'src/app/data/app.data';
import { ElsiDataService } from '../../elsi-data.service';
import { DataTableBaseComponent } from 'src/app/datasets/data-table-base/data-table-base.component';

@Component({
    selector: 'app-elsi-links',
    templateUrl: './elsi-links.component.html',
    styleUrls: ['./elsi-links.component.css']
})
export class ElsiLinksComponent extends DataTableBaseComponent<ElsiLink> {

    constructor(public service: ElsiDataService) {
        super()
        this.dataFilter.sort = { active: 'name', direction: 'asc'};
        this.displayedColumns = ['name','fromZoneStr','toZoneStr','capacity','revCap','loss','market','itf','itt','btf','btt','dummy']
        if ( this.service.datasetInfo) {
            this.createDataSource(this.service.dataset,this.service.datasetInfo.linkInfo)
        } 
        this.addSub( this.service.DatasetInfoChange.subscribe( (ds) => {
            this.createDataSource(this.service.dataset,ds.linkInfo)
        }))
    }
}
