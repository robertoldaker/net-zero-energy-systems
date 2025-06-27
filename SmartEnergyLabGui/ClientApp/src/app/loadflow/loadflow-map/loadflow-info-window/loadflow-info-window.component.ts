import { Component, EventEmitter, OnInit, Output, ViewChild } from '@angular/core';
import { MapInfoWindow } from '@angular/google-maps';

@Component({
    selector: 'app-loadflow-info-window',
    templateUrl: './loadflow-info-window.component.html',
    styleUrls: ['./loadflow-info-window.component.css']
})
export class LoadflowInfoWindowComponent {

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
