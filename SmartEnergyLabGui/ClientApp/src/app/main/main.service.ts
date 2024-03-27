import { Injectable } from '@angular/core';
import { SignalRService } from './signal-r-status/signal-r.service';
import { DataClientService } from '../data/data-client.service';
import { SystemInfo, VersionData } from '../data/app.data';
import { ActivatedRoute, NavigationEnd, Router, RouterEvent } from '@angular/router';
import { filter } from 'rxjs/operators';
import { DialogService } from '../dialogs/dialog.service';

@Injectable({
    providedIn: 'root'
})

export class MainService {

    constructor(private dataClientService: DataClientService, private signalRService: SignalRService, private dialogService: DialogService) { 
        dataClientService.SystemInfo((resp: SystemInfo)=>{
            this.processorCount = resp.processorCount
            this.maintenanceMode = resp.maintenanceMode
            this.serverVersionData = resp.versionData
        })
        signalRService.hubConnection.on('MaintenanceMode',(data) => {
            console.log(`MaintenanceMode ${data}`)
            this.maintenanceMode = data
        })
         
    }

    maintenanceMode: boolean = false
    processorCount: number = 0
    serverVersionData: VersionData | undefined
}
