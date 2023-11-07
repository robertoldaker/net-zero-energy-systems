import { Injectable } from '@angular/core';
import { SignalRService } from '../main/signal-r-status/signal-r.service';
import { EVDemandStatus } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';

@Injectable({
    providedIn: 'root'
})
export class EvDemandService {

    constructor(private signalRService: SignalRService,private dataClientService: DataClientService) { 
        this.dataClientService.GetEVDemandStatus((data)=>{
            this.status=data
            console.log('GetEVDemandStatus')
            console.log(data)
        })
        this.signalRService.hubConnection.on("EVDemandStatus",(data)=>{
            console.log('SignalR EVDemandStatus')
            console.log(data)
            this.status=data
        })
    }

    status:EVDemandStatus = { isReady: false, isRunning: false}

}



