import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { UserService } from 'src/app/users/user.service';
import { LoadflowSplitService } from '../../loadflow/loadflow-split.service';

@Component({
    selector: 'app-elsi-home',
    templateUrl: './elsi-home.component.html',
    styleUrls: ['./elsi-home.component.css']
})
export class ElsiHomeComponent implements OnInit {

    constructor(private splitService: LoadflowSplitService, private userService: UserService, private dialogService: DialogService) {
        
    }

    @ViewChild('leftDiv') leftView: ElementRef | undefined;
    @ViewChild('rightDiv') rightView: ElementRef | undefined;

    ngOnInit(): void {
    }

    splitStart() {

    }

    splitEnd(e: any) {
        this.updateSplitData()
    }

    updateSplitData() {
        let lw = this.leftView?.nativeElement.clientWidth
        let rw = this.rightView?.nativeElement.clientWidth
        this.splitService.updateSplitData(lw,rw)
    }

    get user() {
        return this.userService.user
    }

    get userInfoValid() {
        return this.userService.userInfoValid
    }

    logon() {
        this.dialogService.showLogonDialog()
    }

    register() {
        this.dialogService.showRegisterUserDialog()
    }


}
