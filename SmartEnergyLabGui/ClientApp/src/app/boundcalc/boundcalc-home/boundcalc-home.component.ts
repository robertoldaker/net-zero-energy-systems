import { AfterContentInit, AfterViewInit, Component, ElementRef, HostListener, Inject, OnInit, ViewChild } from '@angular/core';
import { BoundCalcSplitService } from '../boundcalc-split.service';
import { UserService } from 'src/app/users/user.service';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { BoundCalcDataComponent } from '../data/boundcalc-data/boundcalc-data.component';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'app-boundcalc-home',
    templateUrl: './boundcalc-home.component.html',
    styleUrls: ['./boundcalc-home.component.css']
})
export class BoundCalcHomeComponent implements OnInit, AfterViewInit{

    constructor(
        private userService: UserService,
        private dialogService:DialogService,
        @Inject('MODE') private mode: string,
        titleService: Title) {
        let title = 'Bound Calc'
        if (mode !== 'Production') {
            title += ` (${mode})`
        }
        titleService.setTitle(title)
    }

    ngAfterViewInit(): void {
    }

    @ViewChild('leftArea')
    leftArea: ElementRef | undefined;
    @ViewChild('leftDiv')
    leftDiv: ElementRef | undefined;
    @ViewChild('rightDiv')
    rightDiv: ElementRef | undefined;
    @HostListener('window:resize', [])
    onResize() {
        this.resizeDivs()
    }

    leftWidthPx = 410
    leftWidth = this.leftWidthPx + 'px'
    rightWidth = this.getRightWidthStr(this.leftWidthPx)

    getRightWidthStr(lw: number):string {
       let width = window.innerWidth - lw - 11;
       return width + 'px'
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
        this.resizeDivs()
        // this is required to get horizontal scroll bar positioned correctly
        window.setTimeout( ()=>{
            window.dispatchEvent(new Event('resize'));
        },0)
    }

    private resizeDivs() {
        let lw = this.leftArea?.nativeElement.clientWidth
        let rightWidth = this.getRightWidthStr(lw);
        let leftWidth = lw + 'px'
        let leftElement = this.leftDiv?.nativeElement
        if (leftElement) {
            leftElement.style.width = leftWidth
        }
        let rightElement = this.rightDiv?.nativeElement
        if (rightElement) {
            rightElement.style.width = rightWidth
        }
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
