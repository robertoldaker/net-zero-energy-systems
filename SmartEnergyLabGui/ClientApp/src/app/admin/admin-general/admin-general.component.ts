import { Component, OnInit } from '@angular/core';
import { DataClientService } from 'src/app/data/data-client.service';

@Component({
    selector: 'app-admin-general',
    templateUrl: './admin-general.component.html',
    styleUrls: ['./admin-general.component.css']
})
export class AdminGeneralComponent implements OnInit {

    constructor(private dataClientService: DataClientService) {
        
     }

    backupDb() {
        this.dataClientService.BackupDb((result)=>{
            console.log(result)
        })
    }

    startMaintenance() {

    }

    stopMaintenance() {

    }

    loadNetworkData() {
        this.dataClientService.LoadNetworkData((result)=>{
        })
    }

    ngOnInit(): void {
    }

    message: string = "";

}
