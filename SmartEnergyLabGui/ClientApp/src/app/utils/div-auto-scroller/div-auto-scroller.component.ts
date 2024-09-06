import { AfterContentInit, AfterViewInit, Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';

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
            console.log('box.top')
            console.log(box)
            element.style.height = `calc(100vh - ${box.top}px)`
        }
    }

    @ViewChild('divContainer')
    div: ElementRef | undefined;

    @HostListener('window:resize', [])
    onResize() {
        if ( this.div ) {
            //let element = this.div.nativeElement
            //let box = element.getBoundingClientRect()
            //console.log('resize')
            //console.log('box.top')
            //console.log(box.top)
            //??element.style.height = `calc(100vh - ${box.top}px)`
        }
    }



}
