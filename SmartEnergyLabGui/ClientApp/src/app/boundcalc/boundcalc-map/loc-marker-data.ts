import { IMapData, MapOptions } from "src/app/utils/map-options";
import { BoundCalcMapComponent } from "./boundcalc-map.component";
import { BoundCalcLocation, UpdateLocationData } from "../boundcalc-data-service.service";

export class LocMarkerData {
    constructor(private mapComponent: BoundCalcMapComponent) {

    }
    private readonly QB_COLOUR = '#7E4444'
    private readonly LOC_COLOUR = '#aaa'

    private locMarkerData: MapOptions<google.maps.marker.AdvancedMarkerElementOptions,BoundCalcLocation> = new MapOptions()
    private locSvg: HTMLElement | undefined

    get markerOptions():IMapData<google.maps.marker.AdvancedMarkerElementOptions,BoundCalcLocation>[] {

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
            if (mm) {
                // Can't get setting options to work with advanced map markers so access and set map marker options directly
                this.updateAdvancedMarker(mm.advancedMarker,loc)
            } else {
                let options = this.getLocMarkerOptions(loc)
                this.locMarkerData.add(loc.id, options,loc)
            }
        })

    }

    private updateAdvancedMarker( am: google.maps.marker.AdvancedMarkerElement, loc: BoundCalcLocation) {
        var md = this.getLocMarkerData(loc)
        am.position = { lat: loc.gisData.latitude, lng: loc.gisData.longitude}
        if ( am.content ) {
            let svg:any = am.content
            svg.style.setProperty('opacity',md.fillOpacity.toFixed(0))
            svg.style.setProperty('fill', md.fillColor)
        }
        am.title = md.title
        am.gmpDraggable = this.mapComponent.dataService.locationDragging
    }

    updateForZoom() {
        // not zoom dependent so do nothing (for time being anyway)
    }

    getLocMarkerOptions(loc: BoundCalcLocation): google.maps.marker.AdvancedMarkerElementOptions {
        let md = this.getLocMarkerData(loc)
        let locSvg = this.getLocSvg();

        locSvg.style.setProperty('opacity', md.fillOpacity.toFixed(1))
        locSvg.style.setProperty('fill', md.fillColor)

        return {
            position: {
                lat: loc.gisData.latitude,
                lng: loc.gisData.longitude,
            },
            title: md.title,
            content: locSvg,
            zIndex: 15,
            gmpDraggable: this.mapComponent.dataService.locationDragging
        }
    }

    private getLocGenDemandStr(loc: BoundCalcLocation): string {
        let str = ''
        if ( loc.totalDemand!=null) {
            str = `\nD: ${loc.totalDemand.toFixed(0)} MW`
        }
        if ( loc.totalGen!=null) {
            if ( str == '') {
                str = "\n";
            } else {
                str += " "
            }
            str += `G: ${loc.totalGen.toFixed(0)} MW`
        }
        return str
    }

    private getLocMarkerData(loc: BoundCalcLocation) {
        let fillColor = loc.isQB ? this.QB_COLOUR : this.LOC_COLOUR
        let fillOpacity = loc.hasNodes ? 1 : 0.5
        let title =  `${loc.reference}: ${loc.name}` + this.getLocGenDemandStr(loc)
        return { fillColor: fillColor, fillOpacity: fillOpacity, title: title}
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
