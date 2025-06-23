import { Component, Inject, OnInit } from '@angular/core';
import { MapPowerService } from '../map-power.service';
import { DataClientService } from 'src/app/data/data-client.service';
import { EvDemandService } from '../ev-demand.service';

@Component({
    selector: 'app-gsp-info-window',
    templateUrl: './gsp-info-window.component.html',
    styleUrls: ['./gsp-info-window.component.css']
})
export class GspInfoWindowComponent implements OnInit {

    constructor(public mapPowerService: MapPowerService,
                private dataClientService: DataClientService,
                public evDemandService: EvDemandService,
                @Inject('DATA_URL') private baseUrl: string
                ) { }

    ngOnInit(): void {
    }

    runEVDemandTool() {
        if ( this.evDemandService.status.isReady && this.mapPowerService.SelectedGridSupplyPoint) {
            let id = this.mapPowerService.SelectedGridSupplyPoint.id
            this.dataClientService.RunEvDemandGridSupplyPoint(id)
        }
    }

    downloadEvDemandJson() {
        if ( this.mapPowerService.SelectedGridSupplyPoint ) {
            let id = this.mapPowerService.SelectedGridSupplyPoint.id;
            window.location.href = `${this.baseUrl}/EvDemand/Download/GridSupplyPoint?id=${id}`
        }
    }

    get dnoArea():string {
        if (this.mapPowerService.SelectedGridSupplyPoint) {
            let text = MapPowerService.getDNOAreaText(this.mapPowerService.SelectedGridSupplyPoint.dnoArea)
            return text
        } else {
            return ''
        }
    }
}
