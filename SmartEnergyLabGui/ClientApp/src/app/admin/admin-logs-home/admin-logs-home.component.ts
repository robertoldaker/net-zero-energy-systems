import { Component, OnInit } from '@angular/core';
import { DataClientService } from 'src/app/data/data-client.service';
import { EvDemandClientService } from 'src/app/data/ev-demand-client.service';

@Component({
    selector: 'app-admin-logs-home',
    templateUrl: './admin-logs-home.component.html',
    styleUrls: ['./admin-logs-home.component.css']
})
export class AdminLogsHomeComponent implements OnInit {

    constructor(public dataClientService: DataClientService, public evDemandClientService: EvDemandClientService) { }

    ngOnInit(): void {
    }

}
