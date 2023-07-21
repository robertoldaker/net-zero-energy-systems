import { AfterViewInit, Component, OnInit, ViewChild, ViewChildren } from '@angular/core';
import { GoogleMap, MapMarker } from '@angular/google-maps';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { Node, GridSubstation, NodeWrapper, Branch, CtrlWrapper, LoadflowCtrlType, LoadflowLocation, LoadflowBranch } from 'src/app/data/app.data';

@Component({
  selector: 'app-loadflow-map',
  templateUrl: './loadflow-map.component.html',
  styleUrls: ['./loadflow-map.component.css']
})

export class LoadflowMapComponent extends ComponentBase implements OnInit, AfterViewInit {

    @ViewChild(GoogleMap, { static: false }) map: GoogleMap | undefined
    @ViewChildren('nodeMarkers', { read: MapMarker }) nodeMapMarkers: MapMarker[] | undefined


    constructor( private loadflowDataService: LoadflowDataService ) {
        super();

    }

    ngOnInit(): void {

    }

    ngAfterViewInit(): void {
        if ( this.map ) {
            console.log('after view init');
            this.curZoom = this.map.googleMap?.getZoom()
            this.addSub(this.loadflowDataService.LocationDataLoaded.subscribe(()=>{
                // add markers and lines to represent loadflow nodes, branches and ctrls           
                this.addMapData()
            }))  
        }
    }

    zoom = 6
    center: google.maps.LatLngLiteral = {
        //lat: 52.561928, lng: -1.464854
        lat: 54.5255, lng: -1.464854
    }
    options: google.maps.MapOptions = {            
        disableDoubleClickZoom: true,
        mapTypeId: 'roadmap',
        minZoom: 3,
        styles: [
                { featureType: "poi", stylers: [{ visibility: "off" }] }, 
                { featureType: "road", stylers: [{ visibility: "off" }] }, 
                { featureType: "landscape", stylers: [{ visibility: "off" }] },
                { featureType: "administrative", stylers: [{ visibility: "off" }]}],
        mapTypeControl: false,
        scaleControl: true
    }

    curZoom: number | undefined = 0
    zoomTextThreshold : number = 8
    canShowMarkers: boolean = false
    zoomChanged() {
    }


    centerChanged() {

    }

    selectedMarker: MapMarker | null = null

    locMarkerOptions: { options: google.maps.MarkerOptions, id:number }[]=[]
    branchOptions: { options: google.maps.PolylineOptions,  id: number}[]=[]
    addMapData() {
        this.locMarkerOptions = []
        this.loadflowDataService.locationData.locations.forEach(loc => {
            this.addLocMarker(loc)
        })
        this.branchOptions=[];
        this.loadflowDataService.locationData.branches.forEach(branch => {
            this.addBranch(branch)
        })

    }

    addBranch(b: LoadflowBranch) {
        let colour = this.getColour(b);
        this.branchOptions.push( {
            options: {
                path: [
                    {lat: b.gisData1.latitude, lng: b.gisData1.longitude },
                    {lat: b.gisData2.latitude, lng: b.gisData2.longitude },        
                ],
                strokeColor: colour,
                strokeWeight: 1
            },
            id: b.id
        })    
    }

    private getColour(b: LoadflowBranch): string {
        if ( b.voltage==400) {
            return 'blue'
        } else if ( b.voltage==275) {
            return 'red'
        } else {
            return 'black'
        }
    }

    addLocMarker(loc: LoadflowLocation) {
        let image= ( loc.isQB) ? 'loadflowQB.png' : 'loadflowNode.png'
        let icon = {
            url: `/assets/images/${image}`, // url
            scaledSize: new google.maps.Size(40, 40), // scaled size
            origin: new google.maps.Point(0, 0), // origin
            anchor: new google.maps.Point(20, 20), // anchor
        };

        this.locMarkerOptions.push({ 
            options: { 
                icon: icon,
                position: {
                    lat: loc.gisData.latitude,
                    lng: loc.gisData.longitude,
                },
                title: loc.name,
                opacity: 1,
                zIndex: 15
            }, 
            id: loc.id
        } )    
    }

    nodeMarkerClicked(id: number) {

    }

    hvdcMarkerClicked(id: number) {

    }

    branchLineClicked(id: number) {
        console.log(`branchline clicked ${id}`)
    }

    ctrlLineClicked(id: number) {
        console.log(`ctrlline clicked ${id}`)
    }

    mapClick(e: google.maps.MapMouseEvent) {
        console.log(`mapClick lat=${e.latLng?.lat()}, lng=${e.latLng?.lng()}`)
    }
}
