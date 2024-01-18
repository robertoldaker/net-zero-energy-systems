import { Injectable } from '@angular/core';
import { SignalRService } from './signal-r-status/signal-r.service';
import { DataClientService } from '../data/data-client.service';
import { SystemInfo } from '../data/app.data';

@Injectable({
    providedIn: 'root'
})

export class MainService {

    constructor(private dataClientService: DataClientService, private signalRService: SignalRService) { 
        this.version = "1.0.13";
        dataClientService.SystemInfo((resp: SystemInfo)=>{
            this.processorCount = resp.processorCount
            this.maintenanceMode = resp.maintenanceMode
        })
        signalRService.hubConnection.on('MaintenanceMode',(data) => {
            console.log(`MaintenanceMode ${data}`)
            this.maintenanceMode = data
        })
    }

    version: string 
    maintenanceMode: boolean = false
    processorCount: number = 0
}
