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
    iconClass="medium";
    @Input()
    title:string =""

    mouseEnterIcon() {
        console.log('mouseEnterIcon')
        if (this.helpIcon) {
            var element = this.helpIcon.nativeElement
            let box = element.getBoundingClientRect()
            let xPos = box.x - 15
            let yPos = box.y - 10
            this.helpService.showQuickHelp(this.helpId, this.title, xPos, yPos )
        }
    }

    mouseLeaveIcon() {
    }


}
