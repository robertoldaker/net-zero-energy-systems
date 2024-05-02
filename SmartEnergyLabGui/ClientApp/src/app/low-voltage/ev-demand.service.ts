import { Injectable } from '@angular/core';
import { EVDemandStatus } from '../data/app.data';
import { EvDemandClientService } from '../data/ev-demand-client.service';
import { EVDemandSignalRService } from './ev-demand-signalr.service';

@Injectable({
    providedIn: 'root'
})
export class EvDemandService {

    constructor(private signalRService: EVDemandSignalRService,private evClientService: EvDemandClientService) { 
        this.errorMessage = "";
        this.evClientService.GetEVDemandStatus((data)=>{
            this.status=data
        }, (resp)=>{
            this.errorMessage = resp
        })
        this.signalRService.hubConnection.on("EVDemandStatus",(data)=>{
            this.status=data
        })
    }

    status:EVDemandStatus = { isReady: false, isRunning: false}
    errorMessage:  string

}



