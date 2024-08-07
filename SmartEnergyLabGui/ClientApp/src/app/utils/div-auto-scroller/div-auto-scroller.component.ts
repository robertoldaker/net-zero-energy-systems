import { AfterContentInit, AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';

@Component({
    selector: 'app-div-auto-scroller',
    templateUrl: './div-auto-scroller.component.html',
    styleUrls: ['./div-auto-scroller.component.css']
})
export class DivAutoScrollerComponent implements AfterViewInit {

    constructor() { }
    ngAfterViewInit(): void {
        if ( this.div ) {
            let element = this.div.nativeElement
            let box = element.getBoundingClientRect()
            element.style.height = `calc(100vh - ${box.top}px)`
        }
    }

    @ViewChild('divContainer')
    div: ElementRef | undefined;


}
