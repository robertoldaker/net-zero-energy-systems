import { Component, OnInit } from '@angular/core';

@Component({
    selector: 'app-admin-home',
    templateUrl: './admin-home.component.html',
    styleUrls: ['./admin-home.component.css']
})
export class AdminHomeComponent implements OnInit {
    
    constructor() {

    }

    currentTab: string = 'general'

    show(tab: string) {
        this.currentTab = tab
        // dispatch this so that app-div-auto-scroller can detect size change
        window.setTimeout(()=>{window.dispatchEvent(new Event('resize'))},0);        
    }

    isCurrent(tab: string):boolean {
        return this.currentTab==tab
    }

    getClass(tab: string) {
        return {activated: this.isCurrent(tab)};
    }

    ngOnInit(): void {
    }

}
