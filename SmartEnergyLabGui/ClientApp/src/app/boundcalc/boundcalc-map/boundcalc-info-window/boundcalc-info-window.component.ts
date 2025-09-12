import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { MapInfoWindow } from '@angular/google-maps';

@Component({
    selector: 'app-boundcalc-info-window',
    templateUrl: './boundcalc-info-window.component.html',
    styleUrls: ['./boundcalc-info-window.component.css']
})
export class BoundCalcInfoWindowComponent {

    constructor() { }

    closeInfoWindow(e: any) {
        this.closeClick.emit(e)
    }

    @ViewChild(MapInfoWindow) infoWindow: MapInfoWindow | undefined


    @Output()
    closeClick: EventEmitter<any> = new EventEmitter();

    close() {
        this.infoWindow?.close()
    }

    open() {
        this.infoWindow?.open()
    }

    get mapInfoWindow(): MapInfoWindow| undefined {
        return this.infoWindow
    }

}
