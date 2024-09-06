import { Component, OnInit } from '@angular/core';

@Component({
    selector: 'app-elsi-results',
    templateUrl: './elsi-results.component.html',
    styleUrls: ['./elsi-results.component.css']
})
export class ElsiResultsComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
    }

    tabChange(e: any) {
        // dispatch this so that app-div-auto-scroller can detect size change
        window.dispatchEvent(new Event('resize'));
    }
}
