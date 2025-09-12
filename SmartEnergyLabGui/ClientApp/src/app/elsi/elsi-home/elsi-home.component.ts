import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { UserService } from 'src/app/users/user.service';
import { BoundCalcSplitService } from '../../boundcalc/boundcalc-split.service';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'app-elsi-home',
    templateUrl: './elsi-home.component.html',
    styleUrls: ['./elsi-home.component.css']
})
export class ElsiHomeComponent implements OnInit {

    constructor(
        private splitService: BoundCalcSplitService,
        private userService: UserService,
        private dialogService: DialogService,
        titleService: Title) {
        titleService.setTitle('ELSI')
    }

    @ViewChild('leftDiv') leftView: ElementRef | undefined;
    @ViewChild('rightDiv') rightView: ElementRef | undefined;

    ngOnInit(): void {
    }

    splitStart() {

    }

    splitEnd(e: any) {
        this.updateSplitData()
        // dispatch this so that app-div-auto-scroller can detect size change
        window.dispatchEvent(new Event('resize'));
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
