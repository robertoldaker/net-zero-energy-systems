import { Inject, Injectable } from '@angular/core';
import * as signalR from "@microsoft/signalr";

@Injectable({
    providedIn: 'root'
})
export class SignalRService {

    constructor(@Inject('DATA_URL') private baseUrl: string) { 
        let url = baseUrl + '/NotificationHub';

        this.isConnected = false;
        this.hubConnection = new signalR.HubConnectionBuilder().withUrl(url).build();
        // restart connection on close
        this.hubConnection.onclose(()=>{
            this.start();
        });
        //
        this.start();
    }

    start() {
        try {
            this.hubConnection.start()
                .then(()=> {
                    this.isConnected = true;
                })
                .catch( ()=> {
                    this.isConnected = false;
                    setTimeout(()=>{ this.start() }, 5000);
                });
        } catch (error) {
            this.isConnected = false;
            setTimeout(()=>{ this.start() }, 5000);
        }
    }

    isConnected: boolean

    hubConnection: signalR.HubConnection

}
