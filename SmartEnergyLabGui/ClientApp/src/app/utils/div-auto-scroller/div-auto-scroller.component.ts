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
                let windowHeight = window.innerHeight;
                let divHeight = windowHeight - box.top
                element.style.height = `${divHeight}px`
            }
        }
    }



}
