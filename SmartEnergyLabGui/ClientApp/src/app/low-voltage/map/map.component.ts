import { Component, OnInit, AfterViewInit, ViewChild, Inject, OnDestroy, ViewChildren, ElementRef } from '@angular/core'
import { GoogleMap, GoogleMapsModule, MapInfoWindow, MapMarker } from '@angular/google-maps'
import { DistributionSubstation, GISBoundary, GISData, PrimarySubstation, VehicleChargingStation } from '../../data/app.data';
import { MapMarkerComponent } from '../map-marker/map-marker.component';
import { MapDataService} from '../map-data.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { DataClientService } from 'src/app/data/data-client.service';

@Component({
    selector: 'app-map',
    templateUrl: './map.component.html',
    styleUrls: ['./map.component.css']
})

export class MapComponent extends ComponentBase implements OnInit, AfterViewInit {

    @ViewChild(GoogleMap, { static: false }) map: GoogleMap | undefined
    @ViewChild('areaInfoWindow', { read: MapInfoWindow }) areaInfoWindow: MapInfoWindow | undefined;
    @ViewChild('key') key: ElementRef | undefined

    constructor(public mapDataService: MapDataService, private dataClientService: DataClientService) {
        super()
        if ( mapDataService.geographicalArea) {
            this.showGeographicalAreaSelected()
        }
        this.addSub(mapDataService.GeographicalAreaSelected.subscribe(()=>{
            this.showGeographicalAreaSelected()
        }))
    }

    ngOnInit() {
    }

    ngAfterViewInit() {
        if ( this.key ) {
            this.map?.controls[google.maps.ControlPosition.TOP_LEFT].push(this.key.nativeElement);
        }

    }

    // initial zoom and position for UK including Shetland isles
    zoom = 6
    center: google.maps.LatLngLiteral = {
        //??lat: 52.561928, lng: -1.464854        
        lat: 55.6, lng: -1.464854
    }
    options: google.maps.MapOptions = {            
        disableDoubleClickZoom: true,
        mapTypeId: 'roadmap',
        minZoom: this.zoom,
        styles: [{ featureType: "poi", stylers: [{ visibility: "off" }] }, { stylers: [{ gamma: 1.5 }] }],
        mapTypeControl: false,
        scaleControl: true
    }

    areaBoundary: google.maps.LatLngLiteral[] = []
    setBoundaryPoints() {
        if ( this.mapDataService.geographicalArea) {
            this.getBoundaryPoints(this.mapDataService.geographicalArea.gisData, (boundary: google.maps.LatLngLiteral[])=>{
                this.areaBoundary = boundary
            })
        }
    }

    setGeographicalArea(event: any) {
        this.mapDataService.selectGeographicalArea()
    }

    firstCall:boolean = true
    showGeographicalAreaSelected() {
        this.setBoundaryPoints()
        if ( this.areaInfoWindow && !this.firstCall ) {
            let ga = this.mapDataService.geographicalArea
            if ( ga ) {
                this.areaInfoWindow.position = {lat: ga.gisData.latitude, lng: ga.gisData.longitude}
                this.areaInfoWindow.open()    
            }
        }
        this.firstCall = false
    }

    getBoundaryPoints(gisData: GISData, onLoad: ((boundary: google.maps.LatLngLiteral[]) => void | undefined)) {
        this.dataClientService.GetGISBoundaries(gisData.id, (gisBoundaries: GISBoundary[])=>{
            let boundary:google.maps.LatLngLiteral[] = []
            let lats = gisBoundaries[0].latitudes;
            let lngs = gisBoundaries[0].longitudes;
            if ( lats!=null && lngs!=null ) {
                boundary.length = lats.length
                for( let i=0; i<lats?.length;i++) {
                    boundary[i] = { lat: lats[i], lng: lngs[i]};
                }        
            }
            if ( onLoad ) {
                onLoad(boundary)
            }
        })
    } 
    
    zoomChanged() {
        //??console.log(`zoomChanged ${this.map?.googleMap?.getZoom()}`)
    }

    centerChanged(e: any) {
        //??console.log(e)
        //??console.log('centerChanged')
        //??console.log(this.center)
    }

    panToBounds(bounds: google.maps.LatLngBounds) {
        this.map?.panToBounds(bounds);
    }

    panTo(gisData: GISData, minZoom: number) {
        let center = { lat: gisData.latitude, lng: gisData.longitude}

        let curZoom = this.map?.googleMap?.getZoom()
        if ( curZoom && curZoom < minZoom ) {
            this.map?.googleMap?.setZoom(minZoom)
        }

        this.map?.googleMap?.setCenter(center)
    }

    zoomTo(minZoom: number) {
        let curZoom = this.map?.googleMap?.getZoom()
        if ( curZoom && curZoom < minZoom ) {
            this.map?.googleMap?.setZoom(minZoom)
        }
    }

    get currentZoom():number {
        let zoom = this.map?.googleMap?.getZoom();
        if ( zoom ) {
            return zoom;
        } else {
            return 0;
        }
    }

    clearSelection() {
        this.areaInfoWindow?.close()
    }

}
