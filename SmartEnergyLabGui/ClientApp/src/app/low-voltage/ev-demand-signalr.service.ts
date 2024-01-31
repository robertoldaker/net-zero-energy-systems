import { Inject, Injectable } from '@angular/core';
import * as signalR from "@microsoft/signalr";


@Injectable({
    providedIn: 'root'
})

export class EVDemandSignalRService implements signalR.ILogger {

    constructor(@Inject('EV_DEMAND_URL') private baseUrl: string) { 
        let url = baseUrl + '/NotificationHub';

        this.isConnected = false;
        // This stops logging which we get without the EVDemand server running
        this.hubConnection = new signalR.HubConnectionBuilder().configureLogging(this).withUrl(url).build();
        // restart connection on close
        this.hubConnection.onclose(()=>{
            this.start();
        });
        //
        this.start();
    }
    log(logLevel: signalR.LogLevel, message: string): void {
        //?? Still some CORS message that get output so this does not prevent them
        //??console.log(message);
    }

    start() {
        //??
        //?? disabled for time being to prevent spurious error message about CORS from clogging up the console when the Ev Demand service is not running
        return;
        try {
            console.log('start')
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
