import { IMapData, MapOptions } from "src/app/utils/map-options";
import { BoundCalcMapComponent } from "./boundcalc-map.component";
import { MapPolyline } from "@angular/google-maps";
import { BoundCalcLink, UpdateLocationData } from "../boundcalc-data-service.service";
import { Branch } from "src/app/data/app.data";

export class LinkLineData {
    constructor(private mapComponent: BoundCalcMapComponent) {

    }

    //private readonly BOUNDARY_COLOUR = '#00FF2F'
    public static readonly BOUNDARY_COLOUR = '#35A938'

    private linkLineData: MapOptions<google.maps.marker.AdvancedMarkerElementOptions,BoundCalcLink> = new MapOptions()

    get polyLineOptions():IMapData<google.maps.marker.AdvancedMarkerElementOptions,BoundCalcLink>[] {        

        return this.linkLineData.getArray()
    }

    getIndex(id: number) {
        return this.linkLineData.getIndex(id)
    }

    updateAll() {
        for( let linkData of this.linkLineData.getArray()) {
            let link = linkData.data
            linkData.options = this.getBranchOptions(link)
        }        
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

    getBranchOptions(b: BoundCalcLink): google.maps.PolylineOptions {
        let isBoundary = this.mapComponent.dataService.isBoundaryBranch(b.id)
        let options = this.getPolylineOptions(b, false, isBoundary);
        options.path = [
            { lat: b.gisData1.latitude, lng: b.gisData1.longitude },
            { lat: b.gisData2.latitude, lng: b.gisData2.longitude },
        ];
        return options;
    }

    private getPolylineOptions(link: BoundCalcLink, selected: boolean, isBoundary: boolean): google.maps.PolylineOptions {
        let options: google.maps.PolylineOptions = {
            strokeOpacity: 0, // makes it invisible
            strokeWeight: 20 // allows it to be selected when mouse close to line not directly over it
        }

        let firstStrokeOpacity = this.getStrokeOpacity(link.branches[0],selected,isBoundary)
        let secondStrokeOpacity:number | undefined = undefined
        if (link.branches.length>1) {
            secondStrokeOpacity = this.getStrokeOpacity(link.branches[1],selected,isBoundary)
        }

        let lineSymbol: google.maps.Symbol = { path: "" }
        if (link.branches.length > 1) {
            if ( firstStrokeOpacity == secondStrokeOpacity) {
                // does both lines together since opacities are the same
                lineSymbol.path = "M-1,-1 L-1,1 M1,-1 L1,1"
            } else {
                // does first line separately since opacities are different
                lineSymbol.path = "M-1,-1 L-1,1"
            }
        } else {
            lineSymbol.path = "M0,-1 L0,1" // single line
        }
        lineSymbol.strokeOpacity = firstStrokeOpacity
        if (isBoundary) {
            lineSymbol.scale = 3
        } else if (selected) {
            lineSymbol.scale = 2
        } else {
            lineSymbol.scale = 1
        }

        let repeat = ""
        if (link.isHVDC) {
            options.strokeColor = isBoundary ? LinkLineData.BOUNDARY_COLOUR : 'black'
            repeat = selected || isBoundary ? "8px" : "4px"
        } else {
            if (isBoundary) {
                options.strokeColor = LinkLineData.BOUNDARY_COLOUR;
            } else if (link.voltage == 400) {
                options.strokeColor = 'blue'
            } else if (link.voltage == 275) {
                options.strokeColor = 'red'
            } else {
                options.strokeColor = 'black';
            }
            repeat = selected || isBoundary ? "4px" : "2px"
        }
        options.icons = [
            {
                icon: lineSymbol,
                offset: "0",
                repeat: repeat
            }
        ]
        // add second line if multiple branches with different opacities
        if ( link.branchCount>1 && firstStrokeOpacity!=secondStrokeOpacity) {
            let secondLineSymbol: google.maps.Symbol = { path: "" }
            secondLineSymbol.path = "M1,-1 L1,1" // does second offset of double line
            secondLineSymbol.strokeColor = lineSymbol.strokeColor
            secondLineSymbol.strokeOpacity = secondStrokeOpacity
            secondLineSymbol.scale = lineSymbol.scale
            options.icons.push( { icon: secondLineSymbol, offset: "0", repeat: repeat })
        }
        //
        // add arrow if we have some powerFlow available for the link
        if ( link.totalFlow ) {
            let path = link.totalFlow>0 ? google.maps.SymbolPath.FORWARD_CLOSED_ARROW : google.maps.SymbolPath.BACKWARD_CLOSED_ARROW
            let offset = link.totalFlow>0 ? '70%' : '30%'
            const arrowSymbol:google.maps.Symbol = {
                path: path                
            };
            arrowSymbol.strokeOpacity = 1
            arrowSymbol.strokeColor  = lineSymbol.strokeColor
            arrowSymbol.fillColor = lineSymbol.strokeColor
            arrowSymbol.fillOpacity = 1
            arrowSymbol.scale = isBoundary ? (link.branchCount>1 ? 4 : 3) : 1.5

            // 
            options.icons.push( {
                icon: arrowSymbol,
                offset: offset
            })
        }
        return options;
    }

    private getStrokeOpacity(branch: Branch, selected: boolean, isBoundary: boolean):number {
        if (selected || isBoundary) {
            return this.isTripped(branch) ? 0.2 : 1
        } else {
            return this.isTripped(branch) ? 0.2 : 1
        }
    }

    private isTripped(branch: Branch): boolean {
        return this.mapComponent.dataService.isTripped(branch.id) ? true : false
    }
    
}
