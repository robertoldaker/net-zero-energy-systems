import { AfterViewInit, Component, Input, OnInit, ViewChild, ViewChildren } from '@angular/core';
import { GoogleMap, MapInfoWindow, MapMarker } from '@angular/google-maps';
import { DistributionSubstation, GISBoundary, GISData, GridSupplyPoint, PrimarySubstation } from '../../data/app.data';
import { MapMarkerComponent } from '../map-marker/map-marker.component';
import { MapDataService } from '../map-data.service';
import { MapPowerService } from '../map-power.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { MapComponent } from '../map/map.component';
import { DataClientService } from 'src/app/data/data-client.service';

@Component({
    selector: 'app-map-power',
    templateUrl: './map-power.component.html',
    styleUrls: ['./map-power.component.css']
})

export class MapPowerComponent extends ComponentBase implements OnInit {

    @ViewChildren('primaryMarkers', { read: MapMarker }) primaryMapMarkers: MapMarker[] | undefined
    @ViewChildren('gspMarkers', { read: MapMarker }) gspMapMarkers: MapMarker[] | undefined
    @ViewChildren('distMarkers', { read: MapMarker }) distMapMarkers: MapMarker[] | undefined
    @ViewChild('gspInfoWindow', { read: MapInfoWindow }) gspInfoWindow: MapInfoWindow | undefined;
    @ViewChild('primaryInfoWindow', { read: MapInfoWindow }) primaryInfoWindow: MapInfoWindow | undefined;
    @ViewChild('distInfoWindow', { read: MapInfoWindow }) distInfoWindow: MapInfoWindow | undefined;

    constructor(private mapComponent: MapComponent, private mapPowerService: MapPowerService, private dataClientService: DataClientService) {
        super()
        if ( this.mapPowerService.GridSupplyPoints ) {
            this.addGridSupplyPointMarkers()
        }
        this.addSub(this.mapPowerService.GridSupplyPointsLoaded.subscribe(()=>{
            this.addGridSupplyPointMarkers()
        }))
        this.addSub(this.mapPowerService.PrimarySubstationsLoaded.subscribe(()=>{
            this.addPrimaryMarkers()
        }))
        this.addSub(this.mapPowerService.DistributionSubstationsLoaded.subscribe(()=>{
            // add new ones            
            this.addDistributionMarkers()
        }))
        this.addSub(this.mapPowerService.ObjectSelected.subscribe(()=>{
            if ( this.mapPowerService.SelectedPrimarySubstation ) {
                this.showPrimarySelected(this.mapPowerService.SelectedPrimarySubstation);
            } else if ( this.mapPowerService.SelectedDistributionSubstation ) {
                this.showDistSelected(this.mapPowerService.SelectedDistributionSubstation);
            } else if ( this.mapPowerService.SelectedGridSupplyPoint ) {
                this.showGspSelected(this.mapPowerService.SelectedGridSupplyPoint);
            }
        }))        
    }

    ngOnInit(): void {
    }

    @Input()
    map: GoogleMap | undefined

    center: google.maps.LatLngLiteral = {
        lat: 51.381, lng: -2.3590
    }

    gspBoundaries: google.maps.LatLngLiteral[][] = []
    primaryBoundaries: google.maps.LatLngLiteral[][] = []
    distributionBoundaries: google.maps.LatLngLiteral[][] = []

    getBoundaryPoints(gisData: GISData, onLoad: ((boundary: google.maps.LatLngLiteral[][]) => void | undefined)) {

        this.dataClientService.GetGISBoundaries(gisData.id, (gisBoundaries: GISBoundary[])=>{
            let boundaries:google.maps.LatLngLiteral[][] = new Array(gisBoundaries.length)
            for(let i=0;i<gisBoundaries.length;i++) {
                let lats = gisBoundaries[i].latitudes;
                let lngs = gisBoundaries[i].longitudes;
                if ( lats!=null && lngs!=null ) {
                    boundaries[i] = new Array(lats.length)
                    for( let j=0; j<lats?.length;j++) {
                        boundaries[i][j] = { lat: lats[j], lng: lngs[j]};
                    }        
                }
                if ( onLoad ) {
                    onLoad(boundaries);
                }
    
            }
        });
    }

    /*
    ??
    getBounds(gisData: GISData) : google.maps.LatLngBounds {
        let lats = gisData.boundaryLatitudes;
        let lngs = gisData.boundaryLongitudes;
        let minLat = 1000000; let maxLat=-100000
        let minLng = 1000000; let maxLng=-100000
        lats?.forEach(m=> { 
            if ( m<minLat ) {
                minLat = m;
            }
            if ( m>maxLat) {
                maxLat = m
            }
        })
        lngs?.forEach(m=> { 
            if ( m<minLng ) {
                minLng = m;
            }
            if ( m>maxLng) {
                maxLng = m
            }
        })
        let b={ east: minLng, west: maxLng, north: maxLat, south: minLat};
        console.log(b)
        return new google.maps.LatLngBounds(b)
    }
    */


    selectedMarker: MapMarker | null = null

    gspMarkerOptions: { options: google.maps.MarkerOptions, id:number }[]=[]
    addGridSupplyPointMarkers() {
        this.gspMarkerOptions = []
        this.mapPowerService.GridSupplyPoints.forEach(gsp => {
            this.addGridSupplyPointMarker(gsp)
        })
    }
    addGridSupplyPointMarker(gsp: GridSupplyPoint) {
        let anchorOffset = gsp.needsNudge ? 0 : 25;
        let icon = {
            url: "/assets/images/grid-supply-point.png", // url
            scaledSize: new google.maps.Size(50, 50), // scaled size
            origin: new google.maps.Point(0, 0), // origin
            anchor: new google.maps.Point(anchorOffset, 50) // anchor
        };

        this.gspMarkerOptions.push({ 
            options: { 
                icon: icon,
                position: {
                    lat: gsp.gisData.latitude,
                    lng: gsp.gisData.longitude,
                },
                title: `${gsp.name} :${gsp.id.toString()}`,
                opacity: 0.5,
                zIndex: 15
            }, 
            id: gsp.id
        } )
    }

    primaryMarkerOptions: { options: google.maps.MarkerOptions, id:number }[]=[]
    addPrimaryMarkers() {
        this.primaryMarkerOptions = []
        this.mapPowerService.PrimarySubstations.forEach(pss => {
            this.addPrimaryMarker(pss)
        })
    }
    addPrimaryMarker(pss: PrimarySubstation) {
        let icon = {
            url: "/assets/images/primary-substation.png", // url
            scaledSize: new google.maps.Size(40, 40), // scaled size
            origin: new google.maps.Point(0, 0), // origin
            anchor: new google.maps.Point(20, 40) // anchor
        };

        this.primaryMarkerOptions.push({ 
            options: { 
                icon: icon,
                position: {
                    lat: pss.gisData.latitude,
                    lng: pss.gisData.longitude,
                },
                title: `${pss.name} :${pss.id.toString()}`,
                opacity: 0.5,
                zIndex: 10
            }, 
            id: pss.id
        } )
    }

    distMarkerOptions: { options: google.maps.MarkerOptions, id: number }[]=[]
    addDistributionMarkers() {
        this.mapPowerService.DistributionSubstations.forEach(dss => {
            this.addDistributionMarker(dss);
        })
    }

    addDistributionMarker(dss: DistributionSubstation) {
        var icon = {
            url: "/assets/images/distribution-substation.png", // url
            scaledSize: new google.maps.Size(30, 30), // scaled size
            origin: new google.maps.Point(0, 0), // origin
            anchor: new google.maps.Point(15, 30) // anchor
        };
        this.distMarkerOptions.push({ 
            options: { 
                icon: icon,
                position: {
                    lat: dss.gisData.latitude,
                    lng: dss.gisData.longitude,
                },
                title: `${dss.name} :${dss.id.toString()}`,
                opacity: 0.5,
                zIndex: 5
            }, 
            id: dss.id
        } )
    }

    getMarkerId(mapMarker: MapMarker):number {
        let result = 0;
        if ( mapMarker.marker!=undefined) {
            let marker = mapMarker.marker
            let title = marker.getTitle();
            if ( title!=null ) {
                let cpnts = title.split(':')
                let id = parseInt(cpnts[1])
                result = id;        
            }
        }
        return result;
    }

    getAppMapMarker(mapMarkers: MapMarkerComponent[], id: number):MapMarkerComponent|undefined {
        let marker = mapMarkers.find(m=>m.markerOption.id == id);
        return marker;
    }

    getMapMarker(mapMarkers: MapMarker[], id: number):MapMarker|undefined {
        let marker = mapMarkers.find(m=>this.getMarkerId(m)==id);
        return marker;
    }

    gspMarkerClicked(id:number) {
        let selectedGsp = this.mapPowerService.GridSupplyPoints.find(m=>m.id == id);
        //
        this.mapPowerService.setSelectedGridSupplyPoint(selectedGsp);
    }

    primaryMarkerClicked(id:number) {
        let selectedPrimarySubstation = this.mapPowerService.PrimarySubstations.find(m=>m.id == id);
        //
        this.mapPowerService.setSelectedPrimarySubstation(selectedPrimarySubstation);
    }

    distMarkerClicked(e: any, id: number) {
        let selectedDistSubstation = this.mapPowerService.DistributionSubstations.find(m=>m.id == id);
        //
        if ( selectedDistSubstation!=undefined) {
            this.mapPowerService.setSelectedDistributionSubstation(selectedDistSubstation);
        }
    }

    clearSelection() {
        this.selectedMarker?.marker?.setOpacity(0.5)
        this.selectedMarker = null
        this.distInfoWindow?.close()
        this.primaryInfoWindow?.close()
        this.gspInfoWindow?.close()
        this.mapComponent.clearSelection()
    }

    showGspSelected(selectedGsp: GridSupplyPoint) {
        this.clearSelection()
        this.removePrimaryMarkers()
        this.removeDistributionMarkers()
        //
        //let bounds = this.getBounds(selectedGsp.gisData)
        //console.log(bounds)
        this.mapComponent.panTo(selectedGsp.gisData, 10)
        
        //
        if ( this.gspMapMarkers) {
            let mapMarker = this.getMapMarker(this.gspMapMarkers,selectedGsp.id)
            if ( mapMarker!=undefined) {
                mapMarker.marker?.setOpacity(1)
                this.selectedMarker = mapMarker 
                this.gspInfoWindow?.open(mapMarker)
                this.primaryBoundaries = []
                this.distributionBoundaries = []
                this.getBoundaryPoints(selectedGsp.gisData, (boundaries: google.maps.LatLngLiteral[][]) =>{
                    this.gspBoundaries = boundaries
                })
            }    
        }
    }

    showPrimarySelected(selectedPrimary: PrimarySubstation) {
        this.clearSelection();
        this.removeDistributionMarkers();
        this.mapComponent.panTo(selectedPrimary.gisData, 12)
        if ( this.primaryMapMarkers) {
            let mapMarker = this.getMapMarker(this.primaryMapMarkers,selectedPrimary.id)
            if ( mapMarker!=undefined) {
                mapMarker.marker?.setOpacity(1);
                this.selectedMarker = mapMarker;    
                this.primaryInfoWindow?.open(mapMarker);
                this.distributionBoundaries = []
                this.getBoundaryPoints(selectedPrimary.gisData, (boundaries: google.maps.LatLngLiteral[][]) =>{
                    this.primaryBoundaries = boundaries
                })
            }    
        }
    }

    showDistSelected(selectedDist: DistributionSubstation) {
        this.clearSelection();
        this.mapComponent.zoomTo(14)
        if ( this.distMapMarkers) {
            let mapMarker = this.getMapMarker(this.distMapMarkers,selectedDist.id)
            if ( mapMarker!=undefined) {
                // Update opacity of this and any existing selected marker
                mapMarker.marker?.setOpacity(1);
                this.selectedMarker = mapMarker   
                // Add info window
                this.distInfoWindow?.open(mapMarker);
                this.getBoundaryPoints(selectedDist.gisData, (boundaries: google.maps.LatLngLiteral[][]) =>{
                    this.distributionBoundaries = boundaries
                })
            } 
        }
    }

    clearAll() {
        this.clearSelection()
        this.removeDistributionMarkers();
    }

    removeDistributionMarkers() {
        // remove existing ones from map
        this.distMarkerOptions=[];
    }

    removePrimaryMarkers() {
        // remove existing ones from map
        this.primaryMarkerOptions=[];
    }

    get canShowDistMarkers():boolean {
        return this.mapComponent.currentZoom > 11;
    }

    get canShowPrimaryMarkers():boolean {
        return this.mapComponent.currentZoom > 8;
    }
}
