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
    
    GetEVDemandStatus(onLoad: (resp: EVDemandStatus)=>void, onError: (resp: any)=>void) {
        //?? don't use normal way since we need to trap the error message
        //?? this.hsc.GetRequest<EVDemandStatus>("/EvDemand/Status", onLoad)
        let url = "/Admin/Status"
        this.http.get<EVDemandStatus>(this.baseUrl + url).subscribe(resp => {
            if ( onLoad) {
                onLoad(resp);
            }
        },onError)
    }

    RestartEVDemand() {
        this.hsc.GetBasicRequest("/Admin/Restart", ()=>{})
    }
    
    Logs(onLoad: (resp: any)=> void | undefined) {
        this.hsc.GetBasicRequest("/Admin/Logs", onLoad)
    }
}
