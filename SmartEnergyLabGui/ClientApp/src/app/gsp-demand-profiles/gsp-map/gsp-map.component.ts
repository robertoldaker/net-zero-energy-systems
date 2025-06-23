import { AfterViewInit, Component, ElementRef, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { GspDemandProfilesService } from '../gsp-demand-profiles-service';
import { IMapData, MapOptions } from 'src/app/utils/map-options';
import { GridSubstationLocation } from 'src/app/data/app.data';
import { GoogleMap } from '@angular/google-maps';
import { MapAdvancedMarker } from 'src/app/loadflow/loadflow-map/map-advanced-marker/map-advanced-marker';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-gsp-map',
    templateUrl: './gsp-map.component.html',
    styleUrls: ['./gsp-map.component.css']
})
export class GspMapComponent extends ComponentBase implements AfterViewInit {

    constructor(private dataService: GspDemandProfilesService) {
        super()
        if (dataService.locations.length>0) {
            this.locMarkerData.update(dataService.locations);
        }
        this.addSub(dataService.DatesLoaded.subscribe((dates)=>{
        }))
        this.addSub(dataService.LocationsLoaded.subscribe((locs) => {
            this.locMarkerData.update(locs);
        }))
        this.addSub(dataService.LocationSelected.subscribe(() => {
            this.locMarkerData.update(this.dataService.locations)
        }))
    }
    ngAfterViewInit(): void {
        if (this.controls) {
            this.map?.controls[google.maps.ControlPosition.TOP_LEFT].push(this.controls.nativeElement);
        }
    }

    @ViewChild(GoogleMap, { static: false }) map: GoogleMap | undefined
    @ViewChildren('locMarkers', { read: MapAdvancedMarker }) locMapMarkers: QueryList<MapAdvancedMarker> | undefined
    @ViewChild('controls') controls: ElementRef | undefined

    ngOnInit(): void {
    }

    // initial zoom and position for UK including Shetland isles
    zoom = 6
    center: google.maps.LatLngLiteral = {
        //lat: 52.90829, lng: -0.97960 // zoom=7
        lat: 54.50355, lng: -3.76489 // zoom=6
    }
    options: google.maps.MapOptions = {
        disableDoubleClickZoom: true,
        mapTypeId: 'roadmap',
        minZoom: 3,
        // This is defined in google cloud console "https://console.cloud.google.com/google/maps-apis/studio/maps?invt=AbuSBg&project=smart-energy-lab"
        // and defines the style for the map
        mapId: 'b270cc5a1339c548',
        mapTypeControl: false,
        fullscreenControl: false,
        streetViewControl: false,
        scaleControl: true,
        gestureHandling: 'greedy'
    }

    //
    locMarkerData: LocMarkerData = new LocMarkerData(this)

    zoomChanged() {
    }

    centerChanged(e: any) {
    }

    locMarkerClicked(mapData: IMapData<google.maps.marker.AdvancedMarkerElementOptions,GridSubstationLocation>) {
        //this.dataService.selectLocation(mapData.id)
        let loc = mapData.data
        this.dataService.selectLocation(loc)
    }

    get date(): Date | undefined {
        return this.dataService.selectedDate
    }

    set date(value: Date | undefined) {
        this.dataService.selectDate(value)
    }

    get minDate(): Date | undefined {
        return this.dataService.dates.length > 0 ? this.dataService.dates[0] : undefined
    }

    get maxDate(): Date | undefined {
        return this.dataService.dates.length > 0 ? this.dataService.dates[this.dataService.dates.length - 1] : undefined
    }

    isLocSelected(loc: GridSubstationLocation) {
        if ( this.dataService.selectedLocation && this.dataService.selectedLocation.id === loc.id) {
            return true
        } else {
            return false
        }
    }

    isLocGroupSelected(loc: GridSubstationLocation): boolean {
        return this.dataService.isLocationGroupSelected(loc)
    }
}

export class LocMarkerData {
    constructor(private mapComponent: GspMapComponent) {

    }
    private readonly SELECTED_GSP_LOC_COLOUR = 'rgb(230, 82, 1)'
    private readonly SELECTED_GROUP_LOC_COLOUR = 'rgb(105,142,78)'
    private readonly LOC_COLOUR = '#aaa'

    private locMarkerData: MapOptions<google.maps.marker.AdvancedMarkerElementOptions,GridSubstationLocation> = new MapOptions()
    private locSvg: HTMLElement | undefined

    get markerOptions():IMapData<google.maps.marker.AdvancedMarkerElementOptions,GridSubstationLocation>[] {

        return this.locMarkerData.getArray()
    }

    getIndex(id: number) {
        return this.locMarkerData.getIndex(id)
    }


    update(locs: GridSubstationLocation[]) {
        //??this.locMarkerData.clear()

        // replace or add markers as needed
        locs.forEach(loc => {
            let index = this.locMarkerData.getIndex(loc.id)
            let mm = this.mapComponent.locMapMarkers?.get(index)
            if (mm) {
                // Can't get setting options to work with advanced map markers so access and set map marker options directly
                this.updateContent(mm.advancedMarker.content,loc)
            } else {
                let options = this.getLocMarkerOptions(loc)
                this.locMarkerData.add(loc.id, options,loc)
            }
        })

    }

    updateForZoom() {
        // not zoom dependent so do nothing (for time being anyway)
    }

    getLocMarkerOptions(loc: GridSubstationLocation): google.maps.marker.AdvancedMarkerElementOptions {
        let fillColor
        let locSvg = this.getLocSvg();
        this.updateContent(locSvg,loc);
        return {
            position: {
                lat: loc.latitude,
                lng: loc.longitude,
            },
            title: `${loc.reference}: ${loc.name}`,
            content: locSvg,
            zIndex: 15,
            gmpDraggable: false
        }
    }

    updateContent(node: any, loc: GridSubstationLocation) {
        let fillColor
        if (this.mapComponent.isLocSelected(loc)) {
            fillColor = this.SELECTED_GSP_LOC_COLOUR
        } else if (this.mapComponent.isLocGroupSelected(loc)) {
            fillColor = this.SELECTED_GROUP_LOC_COLOUR
        } else {
            fillColor = this.LOC_COLOUR
        }
        let fillOpacity = 1

        node.style.setProperty('opacity', fillOpacity.toFixed(1))
        node.style.setProperty('fill', fillColor)
    }

    getLocSvg(): any {
        if (!this.locSvg) {
            const parser = new DOMParser();
            // A marker with a custom inline SVG.
            const pinSvgString = `<svg xmlns="http://www.w3.org/2000/svg" width="10" height="10"><circle cx="5" cy="5" r="4" /></svg>`
            this.locSvg = parser.parseFromString(pinSvgString, 'image/svg+xml').documentElement;
            this.locSvg.style.setProperty('stroke', 'black')
            this.locSvg.style.setProperty('transform', 'translate(0px, 5px)')
        }
        let locSvg = this.locSvg.cloneNode(true)
        return locSvg;
    }

}
