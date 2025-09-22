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
            this.maintenanceMode = data
        })
        signalRService.hubConnection.on('Ping', () => {
            console.log('ping received')
            this.signalRService.hubConnection.send('Pong')
        })

    }

    maintenanceMode: boolean = false
    processorCount: number = 0
    serverVersionData: VersionData | undefined
    apps: AppInfo[] = [
            { path: '/classificationTool', title: "Classification Tool"},
            { path: '/lowVoltage', title: 'Low voltage network' },
            { path: '/boundCalc', title: 'Transmission Boundary Capability Calculator'},
            { path: '/elsi', title: 'GB Electricity Market Simulator'}
        ]
    pingUsers() {
        this.signalRService.hubConnection.send('PingUsers')
    }
}

export class AppInfo {
    constructor() {
        this.path=''
        this.title=''
    }
    path: string
    title: string
}
