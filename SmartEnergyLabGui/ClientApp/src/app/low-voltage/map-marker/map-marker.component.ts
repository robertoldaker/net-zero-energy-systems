import { Component, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild } from '@angular/core';
import { MapInfoWindow, MapMarker } from '@angular/google-maps';

@Component({
    selector: 'app-map-marker',
    templateUrl: './map-marker.component.html',
    styleUrls: ['./map-marker.component.css']
})
export class MapMarkerComponent implements OnInit, OnDestroy {

    @ViewChild('mapMarker', {read: MapMarker}) mapMarker: MapMarker | undefined;
    @ViewChild('infoWindow', {read: MapInfoWindow}) infoWindow: MapInfoWindow | undefined;

    constructor() {
    }
    ngOnDestroy(): void {
    }

    ngOnInit(): void {

    }



    @Input()
    markerOption: { options: google.maps.MarkerOptions, id: number } | any;

    isSelected:boolean = false

    @Output()
    markerClickedEvent = new EventEmitter<any>()

    markerClicked(e: any) {
        this.markerClickedEvent.emit(e)
    }

}
