import { Component, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
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
export class GspMapComponent extends ComponentBase {

    constructor(private dataService: GspDemandProfilesService) {
        super()
        this.addSub(dataService.DatesLoaded.subscribe((dates)=>{
            console.log('datesLoaded',dates[0],dates[dates.length-1])
        }))
        this.addSub(dataService.LocationsLoaded.subscribe((locs) => {
            console.log('locationsLoaded',locs.length)
            this.locMarkerData.update(locs);
        }))
    }

    @ViewChild(GoogleMap, { static: false }) map: GoogleMap | undefined
    @ViewChildren('locMarkers', { read: MapAdvancedMarker }) locMapMarkers: QueryList<MapAdvancedMarker> | undefined

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

}

export class LocMarkerData {
    constructor(private mapComponent: GspMapComponent) {

    }
    private readonly QB_COLOUR = '#7E4444'
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
        this.locMarkerData.clear()

        // replace or add markers as needed
        locs.forEach(loc => {
            let index = this.locMarkerData.getIndex(loc.id)
            let mm = this.mapComponent.locMapMarkers?.get(index)
            let options = this.getLocMarkerOptions(loc)
            if (mm) {
                // Can't get setting options to work with advanced map markers so access and set map marker options directly
                mm.advancedMarker.position = options.position
                mm.advancedMarker.content = options.content
                if ( options.title ) {
                    mm.advancedMarker.title = options.title
                }
            } else {
                this.locMarkerData.add(loc.id, options,loc)
            }
        })

    }

    updateForZoom() {
        // not zoom dependent so do nothing (for time being anyway)
    }

    getLocMarkerOptions(loc: GridSubstationLocation): google.maps.marker.AdvancedMarkerElementOptions {
        let fillColor = this.LOC_COLOUR
        let fillOpacity = 1
        let locSvg = this.getLocSvg();

        locSvg.style.setProperty('opacity', fillOpacity.toFixed(1))
        locSvg.style.setProperty('fill', fillColor)

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
