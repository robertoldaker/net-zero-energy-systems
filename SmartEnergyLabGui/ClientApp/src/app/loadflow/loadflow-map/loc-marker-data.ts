import { IMapData, MapOptions } from "src/app/utils/map-options";
import { LoadflowMapComponent } from "./loadflow-map.component";
import { LoadflowLocation, UpdateLocationData } from "../loadflow-data-service.service";

export class LocMarkerData {
    constructor(private mapComponent: LoadflowMapComponent) {

    }
    private readonly QB_COLOUR = '#7E4444'
    private readonly LOC_COLOUR = '#aaa'

    private locMarkerData: MapOptions<google.maps.marker.AdvancedMarkerElementOptions,LoadflowLocation> = new MapOptions()
    private locSvg: HTMLElement | undefined

    get markerOptions():IMapData<google.maps.marker.AdvancedMarkerElementOptions,LoadflowLocation>[] {        

        return this.locMarkerData.getArray()
    }

    getIndex(id: number) {
        return this.locMarkerData.getIndex(id)
    }


    update(updateLocationData: UpdateLocationData) {
        if (updateLocationData.clearBeforeUpdate) {
            this.locMarkerData.clear()
        }

        let updateLocs = updateLocationData.updateLocations
        let deleteLocs = updateLocationData.deleteLocations

        // Locations
        // remove markers
        for (let loc of deleteLocs) {
            this.locMarkerData.remove(loc.id)
        }
        // replace or add markers as needed
        updateLocs.forEach(loc => {
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

    getLocMarkerOptions(loc: LoadflowLocation): google.maps.marker.AdvancedMarkerElementOptions {
        let fillColor = loc.isQB ? this.QB_COLOUR : this.LOC_COLOUR
        let fillOpacity = loc.hasNodes ? 1 : 0.5
        let locSvg = this.getLocSvg();

        locSvg.style.setProperty('opacity', fillOpacity.toFixed(1))
        locSvg.style.setProperty('fill', fillColor)

        return {
            position: {
                lat: loc.gisData.latitude,
                lng: loc.gisData.longitude,
            },
            title: `${loc.reference}: ${loc.name}`,
            content: locSvg,
            zIndex: 15,
            gmpDraggable: this.mapComponent.loadflowDataService.locationDragging
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