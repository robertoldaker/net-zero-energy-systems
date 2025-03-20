import { AfterContentInit, AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { LoadflowSplitService } from '../loadflow-split.service';
import { UserService } from 'src/app/users/user.service';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { LoadflowDataComponent } from '../data/loadflow-data/loadflow-data.component';

@Component({
    selector: 'app-loadflow-home',
    templateUrl: './loadflow-home.component.html',
    styleUrls: ['./loadflow-home.component.css']
})
export class LoadflowHomeComponent implements OnInit, AfterViewInit{

    constructor(private splitService: LoadflowSplitService, private userService: UserService, private dialogService:DialogService) {

    }
    ngAfterViewInit(): void {
        this.updateSplitData()        
    }

    @ViewChild('leftDiv') 
    leftView: ElementRef | undefined;
    @ViewChild('rightDiv') 
    rightView: ElementRef | undefined;

    ngOnInit(): void {
    }

    splitStart() {

    }

    splitEnd(e: any) {
        // update data
        this.updateSplitData()
        // also dispatch resize event so other elements can perform any sizing etc. in response
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
