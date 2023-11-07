import { Component, OnInit } from '@angular/core';
import { MapPowerService } from '../map-power.service';
import { DataClientService } from 'src/app/data/data-client.service';

@Component({
    selector: 'app-gsp-info-window',
    templateUrl: './gsp-info-window.component.html',
    styleUrls: ['./gsp-info-window.component.css']
})
export class GspInfoWindowComponent implements OnInit {

    constructor(public mapPowerService: MapPowerService, private dataClientService: DataClientService) { }

    ngOnInit(): void {
    }

    runEVDemandTool() {
        if ( this.mapPowerService.SelectedGridSupplyPoint) {
            let id = this.mapPowerService.SelectedGridSupplyPoint.id
            this.dataClientService.RunEvDemandGridSupplyPoint(id)
        }
    }

}
