import { Component, OnInit } from '@angular/core';
import { DataClientService } from 'src/app/data/data-client.service';
import { EvDemandService } from 'src/app/low-voltage/ev-demand.service';

@Component({
    selector: 'app-admin-general',
    templateUrl: './admin-general.component.html',
    styleUrls: ['./admin-general.component.css']
})
export class AdminGeneralComponent implements OnInit {

    constructor(private dataClientService: DataClientService, public evDemandService: EvDemandService) {
        
    }

    startMaintenance() {

    }

    stopMaintenance() {

    }

    ngOnInit(): void {
    }

    restartEVDemandTool() {
        this.dataClientService.RestartEVDemand();
    }

    message: string = "";

}
