import { Injectable } from '@angular/core';
import { SignalRService } from './signal-r-status/signal-r.service';
import { DataClientService } from '../data/data-client.service';
import { SystemInfo } from '../data/app.data';
import { ActivatedRoute, NavigationEnd, Router, RouterEvent } from '@angular/router';
import { filter } from 'rxjs/operators';
import { DialogService } from '../dialogs/dialog.service';

@Injectable({
    providedIn: 'root'
})

export class MainService {

    constructor(private dataClientService: DataClientService, private signalRService: SignalRService,private router: Router, private dialogService: DialogService) { 
        this.version = "1.0.13";
        dataClientService.SystemInfo((resp: SystemInfo)=>{
            this.processorCount = resp.processorCount
            this.maintenanceMode = resp.maintenanceMode
        })
        signalRService.hubConnection.on('MaintenanceMode',(data) => {
            console.log(`MaintenanceMode ${data}`)
            this.maintenanceMode = data
        })
        console.log('Main service')
        this.router.events.pipe(
            filter((e: any): e is NavigationEnd => {
                return e instanceof NavigationEnd;
            })
         ).subscribe((e: NavigationEnd) => {
           // Do something
            /*console.log('router events')
            if ( e.url.endsWith("/changepassword")) {
                dialogService.showChangePasswordDialog()
            }
            console.log(e)*/
         });
         
    }

    version: string 
    maintenanceMode: boolean = false
    processorCount: number = 0
}
