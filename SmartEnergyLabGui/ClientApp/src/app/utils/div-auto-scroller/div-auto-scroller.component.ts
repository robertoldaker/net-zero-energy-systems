import { AfterContentInit, AfterViewInit, Component, ElementRef, HostListener, Input, OnInit, ViewChild } from '@angular/core';

@Component({
    selector: 'app-div-auto-scroller',
    templateUrl: './div-auto-scroller.component.html',
    styleUrls: ['./div-auto-scroller.component.css']
})
export class DivAutoScrollerComponent implements AfterViewInit {

    constructor() { }

    @Input()
    name: string = "?"

    @Input()
    adjustHeight: boolean = true;

    @Input()
    adjustWidth: boolean = false;

    ngAfterViewInit(): void {
    }

    @ViewChild('divContainer')
    div: ElementRef | undefined;

    @HostListener('window:resize', [])
    onResize() {
        if ( this.div ) {
            let element = this.div.nativeElement
            // this should mean its visible
            if ( element.offsetParent) {
                let box = element.getBoundingClientRect()
                if ( this.adjustHeight) {
                    let windowHeight = window.innerHeight;
                    let divHeight = windowHeight - box.top
                    element.style.height = `${divHeight}px`
                }
                if ( this.adjustWidth) {
                    let windowWidth = window.innerWidth;
                    let divWidth = windowWidth - box.left
                    element.style.width = `${divWidth}px`
                }
            }
        }
    }

    scrollBottom(timeout: number = 0) {
        if ( this.timeoutId === 0) {
            this.timeoutId = window.setTimeout(()=>{
                if ( this.div ) {
                    console.log('scrollTop')
                    let element = this.div.nativeElement
                    element.scrollTop = element.scrollHeight
                }
                this.timeoutId = 0
            }, timeout)
        }
    }

    timeoutId = 0

}
