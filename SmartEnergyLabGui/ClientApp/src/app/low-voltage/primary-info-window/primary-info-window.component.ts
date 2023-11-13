import { Component, OnInit } from '@angular/core';
import { MapPowerService } from '../map-power.service';
import { DataClientService } from 'src/app/data/data-client.service';
import { EvDemandService } from '../ev-demand.service';

@Component({
    selector: 'app-primary-info-window',
    templateUrl: './primary-info-window.component.html',
    styleUrls: ['./primary-info-window.component.css']
})
export class PrimaryInfoWindowComponent implements OnInit {

    constructor(public mapPowerService: MapPowerService, 
        private dataClientService: DataClientService,
        public evDemandService: EvDemandService) {
    }

    ngOnInit(): void {
    }

    runEVDemandTool() {
        if ( this.evDemandService.status.isReady && this.mapPowerService.SelectedPrimarySubstation) {
            let id=this.mapPowerService.SelectedPrimarySubstation.id
            this.dataClientService.RunEvDemandPrimarySubstation(id)
        }
    }

}
