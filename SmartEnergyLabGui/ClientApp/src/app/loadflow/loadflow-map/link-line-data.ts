import { IMapData, MapOptions } from "src/app/utils/map-options";
import { LoadflowMapComponent } from "./loadflow-map.component";
import { ILoadflowLink, UpdateLocationData } from "src/app/data/app.data";
import { MapPolyline } from "@angular/google-maps";

export class LinkLineData {
    constructor(private mapComponent: LoadflowMapComponent) {

    }

    private readonly BOUNDARY_COLOUR = '#00FF2F'

    private linkLineData: MapOptions<google.maps.marker.AdvancedMarkerElementOptions,ILoadflowLink> = new MapOptions()

    get polyLineOptions():IMapData<google.maps.marker.AdvancedMarkerElementOptions,ILoadflowLink>[] {        

        return this.linkLineData.getArray()
    }

    getIndex(id: number) {
        return this.linkLineData.getIndex(id)
    }


    update(updateLocationData: UpdateLocationData) {
        if (updateLocationData.clearBeforeUpdate) {
            this.linkLineData.clear()
        }

        let updateLinks = updateLocationData.updateLinks
        let deleteLinks = updateLocationData.deleteLinks

        // Links
        // delete ones not needed
        for (let link of deleteLinks) {
            this.linkLineData.remove(link.id)
        }
        // replace or add branch options as needed
        updateLinks.forEach(link => {
            let linkOption = this.linkLineData.get(link.id)
            let options = this.getBranchOptions(link)
            if (linkOption) {
                linkOption.options = options
            } else {
                this.linkLineData.add(link.id, options,link)
            }
        })        
    }

    getBranchOptions(b: ILoadflowLink): google.maps.PolylineOptions {
        let options = this.getPolylineOptions(b, false, false);
        options.path = [
            { lat: b.gisData1.latitude, lng: b.gisData1.longitude },
            { lat: b.gisData2.latitude, lng: b.gisData2.longitude },
        ];
        return options;
    }

    private getPolylineOptions(link: ILoadflowLink, selected: boolean, isBoundary: boolean): google.maps.PolylineOptions {
        let options: google.maps.PolylineOptions = {
            strokeOpacity: 0, // makes it invisible
            strokeWeight: 20 // allows it to be selected when mouse close to line not directly over it
        }

        let lineSymbol: google.maps.Symbol = { path: "" }
        if (link.branchCount > 1) {
            lineSymbol.path = "M-1,-1 L-1,1 M1,-1 L1,1" // does a double line
        } else {
            lineSymbol.path = "M0,-1 L0,1" // single line
        }
        if (selected || isBoundary) {
            lineSymbol.strokeOpacity = 1
        } else {
            lineSymbol.strokeOpacity = this.isTripped(link) ? 0.15 : 0.5
        }
        if (isBoundary) {
            lineSymbol.scale = 3
        } else if (selected) {
            lineSymbol.scale = 2
        } else {
            lineSymbol.scale = 1
        }

        if (link.isHVDC) {
            options.strokeColor = isBoundary ? this.BOUNDARY_COLOUR : 'black'
            options.icons = [
                {
                    icon: lineSymbol,
                    offset: "0",
                    repeat: selected || isBoundary ? "8px" : "4px",
                },
            ]
        } else {
            if (isBoundary) {
                options.strokeColor = this.BOUNDARY_COLOUR;
            } else if (link.voltage == 400) {
                options.strokeColor = 'blue'
            } else if (link.voltage == 275) {
                options.strokeColor = 'red'
            } else {
                options.strokeColor = 'black';
            }
            options.icons = [
                {
                    icon: lineSymbol,
                    offset: "0",
                    repeat: selected || isBoundary ? "4px" : "2px",
                },
            ]
        }
        // add arrow if we have some powerFlow available for the link
        if ( link.totalFlow ) {
            let path = link.totalFlow>0 ? google.maps.SymbolPath.FORWARD_CLOSED_ARROW : google.maps.SymbolPath.BACKWARD_CLOSED_ARROW
            let offset = link.totalFlow>0 ? '70%' : '30%'
            const arrowSymbol:google.maps.Symbol = {
                path: path                
            };
            arrowSymbol.strokeOpacity = lineSymbol.strokeOpacity
            arrowSymbol.strokeColor  = lineSymbol.strokeColor
            arrowSymbol.fillColor = lineSymbol.strokeColor
            arrowSymbol.fillOpacity = 1
            arrowSymbol.scale = 2

            // 
            options.icons.push( {
                icon: arrowSymbol,
                offset: offset
            })
        }
        return options;
    }

    private isTripped(link: ILoadflowLink): boolean {
        return link.branches.find(m => this.mapComponent.loadflowDataService.isTripped(m.id)) ? true : false
    }

    boundaryLinks: ILoadflowLink[] = []
    boundaryMapPolylines: Map<number, MapPolyline> = new Map()
    selectBoundaryBranches(boundaryLinks: ILoadflowLink[]) {
        // unselect current ones
        this.boundaryLinks.forEach((branch) => {
            let mapPolyline = this.boundaryMapPolylines.get(branch.id)
            if (mapPolyline) {
                let options = this.getPolylineOptions(branch, false, false)
                mapPolyline.polyline?.setOptions(options);
            }
        })
        // select new ones
        this.boundaryLinks = []
        this.boundaryMapPolylines.clear()
        // make a copy of array
        this.boundaryLinks = [...boundaryLinks]
        this.boundaryLinks.forEach((branch) => {
            var index = this.linkLineData.getIndex(branch.id);
            if (index >= 0 && this.mapComponent.linkMapPolylines) {
                let mapPolyline = this.mapComponent.linkMapPolylines.get(index);
                if (mapPolyline) {
                    let options = this.getPolylineOptions(branch, false, true)
                    mapPolyline.polyline?.setOptions(options);
                    this.boundaryMapPolylines.set(branch.id, mapPolyline)
                }
            }
        })

    }
    
}
