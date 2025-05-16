import { GISData } from "src/app/data/app.data";
import { IMapData, MapOptions } from "src/app/utils/map-options";
import { LoadflowMapComponent, MapFlowFilter } from "./loadflow-map.component";
import { LoadflowLink, PercentCapacityThreshold, UpdateLocationData } from "../loadflow-data-service.service";

export class LinkLabelData {
    constructor(private mapComponent: LoadflowMapComponent) {

    }
    private linkLabelData: MapOptions<google.maps.marker.AdvancedMarkerElementOptions,LoadflowLink> = new MapOptions()
    get markerOptions():IMapData<google.maps.marker.AdvancedMarkerElementOptions,LoadflowLink>[] {        
        return this.linkLabelData.getArray()
    }

    update(updateLocationData: UpdateLocationData) {
    
        if (updateLocationData.clearBeforeUpdate) {
            this.linkLabelData.clear()
        }

        let updateLinks = updateLocationData.updateLinks
        let deleteLinks = updateLocationData.deleteLinks

        // Links
        // delete ones not needed
        for (let link of deleteLinks) {
            this.linkLabelData.remove(link.id)
        }
        // replace or add branch options as needed
        let tol = this.getDisplayTol()
        updateLinks.forEach(link => {
            //
            let linkLabelOption = this.linkLabelData.get(link.id)
            let options = this.getLinkLabelOptions(link, tol)
            if ( linkLabelOption ) {
                //
                let index = this.linkLabelData.getIndex(link.id)
                let amm = this.mapComponent.linkMarkers?.get(index)
                if ( amm ) {
                    // Can't get setting options to work with advanced map markers so access and set map marker options directly
                    amm.advancedMarker.position = options.position
                    amm.advancedMarker.content = options.content
                    amm.advancedMarker.zIndex = options.zIndex
                    if ( options.title ) {
                        amm.advancedMarker.title = options.title
                    }
                }
            } else {
                this.linkLabelData.add(link.id, options , link)
            }
        })

    }

    updateForZoom() {
        //
        let tol = this.getDisplayTol()
        //
        for( let linkData of this.linkLabelData.getArray()) {
            let link = linkData.data
            let index = this.linkLabelData.getIndex(linkData.id)
            let amm = this.mapComponent.linkMarkers?.get(index)
            if ( amm ) {
                let element:any = amm.advancedMarker.content
                let linkInfo = this.getLinkInfo(link,tol)
                element.className = linkInfo.cn                
            }
        }
    } 
    
    private getLinkLabelOptions(link: LoadflowLink, tol: number): google.maps.marker.AdvancedMarkerElementOptions {
        const linkLabelDiv = document.createElement('div');

        let linkInfo = this.getLinkInfo(link,tol)
        linkLabelDiv.className = linkInfo.cn
        linkLabelDiv.textContent = linkInfo.label
        let lat = (link.gisData1.latitude + link.gisData2.latitude)/2;
        let lng = (link.gisData1.longitude + link.gisData2.longitude)/2;
        return {
            position: {
                lat: lat,
                lng: lng,
            },
            content: linkLabelDiv,            
            zIndex: linkInfo.zIndex,
            gmpDraggable: false
        }
    }

    private getDisplayTol(): number {
        // get zoom level
        let zoom = this.mapComponent.googleMap?.getZoom();
        if ( !zoom && zoom!=0 ) {
            return 0;
        }
        // tolerance based on the inverse square of the zoom level
        // https://medium.com/google-design/google-maps-cb0326d165f5
        let tol = 20 /Math.pow(2,zoom);
        // square tolerance since we are getting the distance squared to save lots of sqrts
        tol = tol*tol;
        //
        return tol
    }

    private getLabel(link: LoadflowLink):string {
        let label = link.totalFlow!=null ? Math.abs(link.totalFlow).toFixed(0) : ''
        return label
    }

    private getLinkInfo(link: LoadflowLink, tol: number):{cn: string, zIndex: number, label: string } {
        let label = this.getLabel(link)
        let ds = this.mapComponent.dataService
        let flowFilter = this.mapComponent.flowFilter
        if ( label && ds.isBoundaryLimCct(link)) {
            label = `*${label}*`
        }
        let percentThreshold = ds.getFlowCapacityThreshold(link.type, link.percentCapacity)
        let showLabel = this.showLabel(link, flowFilter, percentThreshold)
        let className:string,zIndex:number
        if ( showLabel && label) {
            if ( percentThreshold == PercentCapacityThreshold.OK) {
                className = "flowLabel"
                // work out if we should show based on the length of the link and a tolerance
                if ( flowFilter == MapFlowFilter.All) {
                    let distSqr = this.getPixelDistanceSqr(link.gisData1,link.gisData2)
                    if ( distSqr <= tol ) {
                        className = "hide"
                    }
                } 
                zIndex = 15
            } else if ( percentThreshold == PercentCapacityThreshold.Warning ) {
                className = "flowLabel notOK warning"
                zIndex = 20
            } else if ( percentThreshold == PercentCapacityThreshold.Critical ) {
                className = "flowLabel notOK critical"
                zIndex = 25
            } else {
                throw `Unexpected value for percentThreshold [${percentThreshold}]`
            }
        } else {
            className = "hide"
            zIndex = 15
        }
        //
        return {cn: className, zIndex: zIndex, label: label}
    }

    private showLabel(link: LoadflowLink, flowFilter: MapFlowFilter, percentThreshold: PercentCapacityThreshold) {
        let ds = this.mapComponent.dataService
        if ( flowFilter === MapFlowFilter.All ) {
            return true;
        } else if ( flowFilter === MapFlowFilter.Warning) {
            return (percentThreshold === PercentCapacityThreshold.Warning || percentThreshold === PercentCapacityThreshold.Critical)
        } else if ( flowFilter === MapFlowFilter.Critical) {
            return percentThreshold === PercentCapacityThreshold.Critical
        } else if ( flowFilter === MapFlowFilter.Boundary)  {
            return ds.isBoundaryLink(link) || ds.isBoundaryLimCct(link)
        } else {
            throw `Unexpected value for MapFlowFilter [${flowFilter}]`
        }
    }

    private getPixelDistanceSqr(point1: GISData, point2: GISData):number {
        // Convert lat/lng to pixel coordinates
        let map = this.mapComponent.googleMap
        let distanceSqr = 0
        if ( map ) {
            const proj = map.getProjection()
            if ( proj ) {
                const pixel1 = proj.fromLatLngToPoint( { lat: point1.latitude, lng: point1.longitude});
                const pixel2 = proj.fromLatLngToPoint({ lat: point2.latitude, lng: point2.longitude});
                if (pixel1 && pixel2) {
                    // Calculate the distance between the pixel coordinates
                    const dx = pixel1.x - pixel2.x;
                    const dy = pixel1.y - pixel2.y;
                    distanceSqr = dx * dx + dy * dy;

                }              
            }
        } 
        return distanceSqr
    }
    
}