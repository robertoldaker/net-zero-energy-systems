import { Component, OnInit } from '@angular/core';

@Component({
    selector: 'app-gsp-home',
    templateUrl: './gsp-home.component.html',
    styleUrls: ['./gsp-home.component.css']
})
export class GspHomeComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
    }

    splitStart() {
        // this gets read by e-charts wrapper which will react to this and redraw
        console.log('split start')
        window.dispatchEvent(new Event('resize'));
    }
    splitEnd() {
        console.log('split end')
        // this gets read by e-charts wrapper which will react to this and redraw
        window.dispatchEvent(new Event('resize'));
    }

}
