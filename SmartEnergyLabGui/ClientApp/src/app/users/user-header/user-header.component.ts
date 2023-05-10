import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { User, UserRole } from '../../data/app.data';
import { DialogService } from '../../dialogs/dialog.service';
import { UserService } from '../user.service';

@Component({
    selector: 'app-user-header',
    templateUrl: './user-header.component.html',
    styleUrls: ['./user-header.component.css']
})
export class UserHeaderComponent implements OnInit, OnDestroy {

    private subs: Subscription[]
    constructor(private dialogService: DialogService, public userService: UserService) { 
        this.subs = [];
    }

    ngOnDestroy(): void {
        this.subs.forEach(m=>m.unsubscribe())
    }

    ngOnInit(): void {
    }

    logon() {
        this.dialogService.showLogonDialog();
    }

    logoff() {
        this.userService.logOff();
    }

    register() {
        this.dialogService.showRegisterUserDialog()
    }

    changePassword() {
        this.dialogService.showChangePasswordDialog()
    }

    get user() : User | undefined {
        return this.userService.user
    }

}

