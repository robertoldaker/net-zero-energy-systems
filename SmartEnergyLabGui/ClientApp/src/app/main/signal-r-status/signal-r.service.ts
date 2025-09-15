import { Inject, Injectable } from '@angular/core';
import * as signalR from "@microsoft/signalr";
import { UserService } from 'src/app/users/user.service';

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
            console.log('Connection closed')
            this.start();
        });
        //
        this.start();
        //
    }

    start() {
        try {
            console.log('starting SignalR ...')
            this.hubConnection.start()
                .then(()=> {
                    this.isConnected = true;
                    console.log('started')
                })
                .catch( (e)=> {
                    console.log('catch 1 stopped',e)
                    this.isConnected = false;
                    setTimeout(()=>{ this.start() }, 5000);
                });
        } catch (error) {
            console.log('catch 2 stopped', error)
            this.isConnected = false;
            setTimeout(()=>{ this.start() }, 5000);
        }
    }

    restart() {
        // Note since the onclose will restart automatically only need to stop connection
        try {
            this.hubConnection.stop().then(()=> {
                console.log('stopped ',this.hubConnection.state)
                this.isConnected = false;
            })
            .catch( ()=>{
                console.log('Problem stopping signalR connection')
            })
        } catch( error) {
            console.log(`Exception stopping signalR connection [${error}]`)
        }
    }

    isConnected: boolean
    hubConnection: signalR.HubConnection
    get connectionId():string {
        return this.isConnected && this.hubConnection.connectionId ? this.hubConnection.connectionId : ''
    }

}
