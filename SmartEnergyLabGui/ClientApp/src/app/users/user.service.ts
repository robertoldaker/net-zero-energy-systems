import { EventEmitter, Injectable } from '@angular/core';
import { User } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';

@Injectable({
    providedIn: 'root'
})
export class UserService {

    constructor(private service: DataClientService) {
        this.checkLogon()
    }

    checkLogon() {
        this.service.CurrentUser((user)=> {
            this.user = user;
            this.userInfoValid = true
            if ( this.user ) {
                this.LogonEvent.emit(this.user)
            }
        })
    }

    logOff() {
        this.service.Logoff((resp)=>{
            this.user = undefined;
            this.LogoffEvent.emit() 
        })
    }

    user: User | undefined
    userInfoValid: boolean = false
    LogonEvent: EventEmitter<User> = new EventEmitter<User>()
    LogoffEvent: EventEmitter<any> = new EventEmitter<any>()
}
