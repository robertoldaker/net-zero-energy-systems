import { AfterViewInit, Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';
import { QuickHelpInfo, QuickHelpService } from './quick-help.service';

@Component({
    selector: 'app-quick-help',
    templateUrl: './quick-help.component.html',
    styleUrls: ['./quick-help.component.css']
})
export class QuickHelpComponent  {

    constructor(private helpService: QuickHelpService) {
    }

    @ViewChild('helpIcon')
    helpIcon: ElementRef | undefined

    @Input()
    helpId: string = ""
    @Input()
    iconStyle = {}
    @Input()
    iconClass="small";
    @Input()
    title:string =""

    private timeoutId: number = 0
    mouseEnterIcon() {
        if (this.helpIcon) {
            var element = this.helpIcon.nativeElement
            let box = element.getBoundingClientRect()
            let xPos = box.x - 15
            let yPos = box.y - 10
            // clear any that haven;t been fired yet
            if ( this.timeoutId) {
                window.clearTimeout(this.timeoutId)
            }
            this.timeoutId = window.setTimeout( ()=>{
                this.timeoutId = 0
                this.helpService.showQuickHelp(this.helpId, this.title, xPos, yPos)
            }, 300 )
        }
    }

    mouseLeaveIcon() {
        // Cancel any pending timeouts. This allows the user to quickly mouse over
        // icon and cause the quickHelp to be displayed
        if ( this.timeoutId) {
            window.clearTimeout(this.timeoutId)
        }
    }


}
