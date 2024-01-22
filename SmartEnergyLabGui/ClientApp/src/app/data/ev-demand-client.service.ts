import { Inject, Injectable } from '@angular/core';
import { EVDemandStatus } from './ev-demand-data';
import { HttpClient } from '@angular/common/http';
import { ShowMessageService } from '../main/show-message/show-message.service';
import { HttpServiceClient } from './http-service-client';

@Injectable({
    providedIn: 'root'
})
export class EvDemandClientService {

    constructor(
        private http: HttpClient, 
        showMessageService: ShowMessageService,
        @Inject('EV_DEMAND_URL') private baseUrl: string
    ) { 
        this.hsc = new HttpServiceClient(baseUrl,http,showMessageService)
    }

    private hsc: HttpServiceClient
    
    GetEVDemandStatus(onLoad: (resp: EVDemandStatus)=>void, onError?: ((resp: string)=>void)) {
        this.hsc.GetBasicRequest("/Admin/Status", onLoad, onError)
    }

    RestartEVDemand() {
        this.hsc.GetBasicRequest("/Admin/Restart",()=>{})
    }
    
    Logs(onLoad: (resp: any)=> void | undefined, onError: (resp: string)=>void) {
        this.hsc.GetBasicRequest("/Admin/Logs", onLoad, onError)
    }
}
