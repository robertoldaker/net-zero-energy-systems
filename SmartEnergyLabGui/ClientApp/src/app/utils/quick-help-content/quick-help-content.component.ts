import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { QuickHelpService } from '../quick-help/quick-help.service';
import { ComponentBase } from '../component-base';

@Component({
    selector: 'app-quick-help-content',
    templateUrl: './quick-help-content.component.html',
    styleUrls: ['./quick-help-content.component.css']
})
export class QuickHelpContentComponent extends ComponentBase {

    constructor(public service: QuickHelpService) {
        super()
        this.addSub(service.ShowQuickHelpEvent.subscribe( ()=>{
            if ( this.helpTextDiv ) {
                var element = this.helpTextDiv.nativeElement
                let xPos = service.xPos
                let xPosShifted = false
                // This ensures we go to the maximum width (without it the div will not size beyond the right edge of the window)
                if ( xPos + 520 > window.innerWidth) {
                    xPos = window.innerWidth - 520
                    xPosShifted = true
                }
                // try left aligned
                let top = `${service.yPos.toFixed(0)}px`
                let left = `${xPos.toFixed(0)}px`
                this.contentStyle = { top: top, left: left }
                // need to re-check on the next gui cycle to see the actual width of the div
                window.setTimeout( ()=>{
                    let box = element.getBoundingClientRect()
                    // now see if we cannot fit in the div when using the correct xPos from the service
                    if ( service.xPos + box.width > window.innerWidth) {
                        let right = '20px'
                        this.contentStyle = { top: top, right: right }
                    } else if ( xPosShifted ) {
                        let left = `${service.xPos.toFixed(0)}px`
                        this.contentStyle = { top: top, left: left }
                    }
                },0)
            }
        }))
    }

    @ViewChild('helpTextDiv')
    helpTextDiv: ElementRef | undefined

    contentStyle={}

    mouseEnterText() {
    }

    mouseLeaveText() {
        this.service.clearHelp()
    }

}
