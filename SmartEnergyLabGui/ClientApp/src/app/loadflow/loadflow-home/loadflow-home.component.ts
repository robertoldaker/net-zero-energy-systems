import { AfterContentInit, AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { LoadflowSplitService } from '../loadflow-split.service';
import { UserService } from 'src/app/users/user.service';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { LoadflowDataComponent } from '../data/loadflow-data/loadflow-data.component';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'app-loadflow-home',
    templateUrl: './loadflow-home.component.html',
    styleUrls: ['./loadflow-home.component.css']
})
export class LoadflowHomeComponent implements OnInit, AfterViewInit{

    constructor(
        private splitService: LoadflowSplitService,
        private userService: UserService,
        private dialogService:DialogService,
        titleService: Title) {
            titleService.setTitle('Bound Calc')
    }
    ngAfterViewInit(): void {
        window.setTimeout(() => {
            window.dispatchEvent(new Event('resize'));
        }, 1000)
    }

    @ViewChild('leftDiv')
    leftView: ElementRef | undefined;
    @ViewChild('rightDiv')
    rightView: ElementRef | undefined;

    leftWidthPx = 410
    leftWidth = this.leftWidthPx + 'px'
    rightWidth = this.getRightWidthStr(this.leftWidthPx)

    getRightWidthStr(lw: number):string {
        return `calc(100vw - ${lw + 11}px)`
    }

    ngOnInit(): void {
    }

    splitStart() {

    }

    splitEnd(e: any) {
        // update data
        this.updateSplitData()
    }

    updateSplitData() {
        window.setTimeout(()=>{
            let lw = this.leftView?.nativeElement.clientWidth
            let rw = this.rightView?.nativeElement.clientWidth
            this.splitService.updateSplitData(lw, rw)
            this.rightWidth = this.getRightWidthStr(lw);
            this.leftWidth = lw + 'px'
            // this is required to get horizontal scroll bar positioned correctly
            window.setTimeout( ()=>{
                window.dispatchEvent(new Event('resize'));
            },0)
        },0)
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
