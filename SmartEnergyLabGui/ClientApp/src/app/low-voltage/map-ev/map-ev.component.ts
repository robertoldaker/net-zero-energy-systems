import { Component, OnDestroy, OnInit, ViewChildren } from '@angular/core';
import { MapInfoWindow, MapMarker } from '@angular/google-maps';
import { VehicleChargingStation } from '../../data/app.data';
import { MapMarkerComponent } from '../map-marker/map-marker.component';
import { MapDataService } from '../map-data.service';
import { MapEvService } from '../map-ev.service';

@Component({
    selector: 'app-map-ev',
    templateUrl: './map-ev.component.html',
    styleUrls: ['./map-ev.component.css']
})
export class MapEvComponent implements OnInit, OnDestroy {

    private subs1: any;
    private subs2: any;
    private subs3: any;
    constructor(private mapEvService: MapEvService, private mapDataService: MapDataService) {
        this.addChargingMarkers();        
        this.subs1 = this.mapEvService.VehicleChargingStationsLoaded.subscribe(()=>{
            this.addChargingMarkers();
        })
        this.subs2 = this.mapEvService.ObjectSelected.subscribe(()=>{
            if ( this.mapEvService.selectedVehicleChargingStation) {
                this.showChargingSelected(this.mapEvService.selectedVehicleChargingStation);
            }
        })
        this.subs3 = this.mapDataService.GeographicalAreaSelected.subscribe(()=>{
        });
    }
    @ViewChildren('chargingAppMapMarkers', { read: MapMarkerComponent}) chargingAppMapMarkers: MapMarkerComponent[] | undefined
    
    ngOnDestroy(): void {
        this.subs1.unsubscribe()
        this.subs2.unsubscribe()
        this.subs3.unsubscribe()
    }

    ngOnInit(): void {
    }

    chargingMarkerOptions: { options: google.maps.MarkerOptions, id: number }[] = []
    addChargingMarkers() {
        this.chargingMarkerOptions = [];
        this.mapEvService.vehicleChargingStations.forEach(vcs => {
            this.addChargingMarker(vcs);
        })
    }

    addChargingMarker(vcs: VehicleChargingStation) {
        var icon = {
            url: "/assets/images/vehicle-charging-station.png", // url
            scaledSize: new google.maps.Size(30, 30), // scaled size
            origin: new google.maps.Point(0, 0), // origin
            anchor: new google.maps.Point(0, 0) // anchor
        };
        this.chargingMarkerOptions.push({
            options: {
                icon: icon,
                position: {
                    lat: vcs.gisData.latitude,
                    lng: vcs.gisData.longitude,
                },
                title: `${vcs.name} :${vcs.id.toString()}`,
                opacity: 0.5,
                zIndex: 5
            },
            id: vcs.id
        })
    }

    chargerMarkerClicked(e: any, id: number) {
        let selectedObj = this.mapEvService.vehicleChargingStations.find(m=>m.id == id);
        //
        if ( selectedObj!=undefined) {
            this.mapEvService.setSelectedVehicleChargingStation(selectedObj)
        }
    }

    getAppMapMarker(mapMarkers: MapMarkerComponent[], id: number):MapMarkerComponent|undefined {
        let marker = mapMarkers.find(m=>m.markerOption.id == id);
        return marker;
    }

    selectedAppMarker: MapMarkerComponent | undefined = undefined
    clearSelection() {
        this.selectedAppMarker?.mapMarker?.marker?.setOpacity(0.5);
        this.selectedAppMarker?.infoWindow?.close();
        this.selectedAppMarker = undefined;
    }

    showChargingSelected(selectedObj: VehicleChargingStation) {
        this.clearSelection();
        if ( this.chargingAppMapMarkers) {
            let appMapMarker = this.getAppMapMarker(this.chargingAppMapMarkers,selectedObj.id)
            if ( appMapMarker!=undefined) {
                // Update opacity of this and any existing selected marker
                this.selectedAppMarker = appMapMarker;
                appMapMarker.mapMarker?.marker?.setOpacity(1);
                // Add info window
                appMapMarker.infoWindow?.open(appMapMarker.mapMarker);
            }  
        }
    }

}
