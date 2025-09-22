import { EventEmitter, Injectable } from '@angular/core';
import { User, UserRole } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';
import { SignalRService } from '../main/signal-r-status/signal-r.service';

@Injectable({
    providedIn: 'root'
})
export class UserService {

    constructor(private service: DataClientService, private signalRService: SignalRService) {
        //
        this.checkLogon()
        //
        window.addEventListener('storage', (event) => {
            // Check if the change was to our 'isLoggedIn' key
            if (event.key === this.storageKey) {
                console.log('Login/logoff detected from another tab!');
                // Redirect or update the UI to reflect the logged-in state
                window.location.reload(); // Or update the UI dynamically
            }
        });
    }

    checkLogon() {
        this.service.CurrentUser((user)=> {
            this.user = user;
            this.userInfoValid = true
            if ( this.user ) {
                //
                this.LogonEvent.emit(this.user)
                //
            }
        })
    }

    logOff() {
        this.service.Logoff(this.signalRService.connectionId, (resp)=>{
            this.user = undefined;
            //
            this.LogoffEvent.emit()
            //
            this.notifyOtherTabs()
            //
            window.location.reload()
        })
    }

    private readonly storageKey = 'loggedInChange'

    notifyOtherTabs() {
        let item = localStorage.getItem(this.storageKey)
        if ( item && item == '1') {
            localStorage.setItem(this.storageKey, '0');
        } else {
            localStorage.setItem(this.storageKey, '1');
        }
    }

    get isAdmin() {
        return this.user?.role == UserRole.Admin
    }

    user: User | undefined
    userInfoValid: boolean = false
    LogonEvent: EventEmitter<User> = new EventEmitter<User>()
    LogoffEvent: EventEmitter<any> = new EventEmitter<any>()
}
