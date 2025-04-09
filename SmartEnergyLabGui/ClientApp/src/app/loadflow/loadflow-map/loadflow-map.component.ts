import { AfterViewInit, Component, ElementRef, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { GoogleMap, MapInfoWindow, MapMarker, MapPolyline } from '@angular/google-maps';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService, LoadflowLink, LoadflowLocation, SelectedMapItem } from '../loadflow-data-service.service';
import { BoundaryTrip, BoundaryTripType, ILoadflowLink, ILoadflowLocation, UpdateLocationData } from 'src/app/data/app.data';
import { IMapData, MapOptions } from 'src/app/utils/map-options';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DatasetsService, NewItemData } from 'src/app/datasets/datasets.service';
import { DataClientService } from 'src/app/data/data-client.service';
import { IFormControlDict } from 'src/app/dialogs/dialog-base';
import { MapAdvancedMarker } from './map-advanced-marker/map-advanced-marker';

@Component({
    selector: 'app-loadflow-map',
    templateUrl: './loadflow-map.component.html',
    styleUrls: ['./loadflow-map.component.css']
})
export class LoadflowMapComponent extends ComponentBase implements OnInit, AfterViewInit {

    @ViewChild(GoogleMap, { static: false }) map: GoogleMap | undefined
    @ViewChildren('locMarkers', { read: MapAdvancedMarker }) locMapMarkers: QueryList<MapAdvancedMarker> | undefined
    @ViewChildren('linkLines', { read: MapPolyline }) linkMapPolylines: QueryList<MapPolyline> | undefined
    @ViewChild('key') key: ElementRef | undefined
    @ViewChild('locInfoWindow', { read: MapInfoWindow }) locInfoWindow: MapInfoWindow | undefined
    @ViewChild('linkInfoWindow', { read: MapInfoWindow }) linkInfoWindow: MapInfoWindow | undefined
    @ViewChild('divContainer') divContainer: ElementRef | undefined
    @ViewChild('dummyMarker', { read: MapMarker }) dummyMarker: MapMarker | undefined

    constructor(
        private loadflowDataService: LoadflowDataService, 
        private messageService: ShowMessageService,
        private dialogService: DialogService,
        private dataService: DataClientService,
        private datasetsService: DatasetsService
    ) {
        super();
        this.addSub(this.loadflowDataService.LocationDataUpdated.subscribe((updateLocationData) => {
            // add markers and lines to represent loadflow nodes, branches and ctrls 
            this.updateMapData(updateLocationData)
        }))
        this.addSub(this.loadflowDataService.BoundarySelected.subscribe((boundaryLinks) => {
            // select links associated with the selected boundary
            this.selectBoundaryBranches(boundaryLinks)
        }))
        this.addSub(this.loadflowDataService.ObjectSelected.subscribe((selectedItem) => {
            // select an objet (link or location)
            this.selectObject(selectedItem)
        }))
    }

    addBranchHandler: AddBranchHandler | undefined
    addLocationHandler: AddLocationHandler | undefined
    // Needed to support opening of location info window whilst using advanced markers ith a older version of angular/google-maps
    dummyMarkerOptions: google.maps.MarkerOptions = { position: {lat: 54.50355, lng: -3.76489}, visible: false  }
    private locSvg: HTMLElement | undefined

    ngOnInit(): void {

    }

    ngAfterViewInit(): void {
        if (this.key) {
            this.map?.controls[google.maps.ControlPosition.TOP_LEFT].push(this.key.nativeElement);
        }
        if ( this.locInfoWindow ) {
            this.addBranchHandler = new AddBranchHandler(this.locInfoWindow,this.messageService,this.dialogService, this.loadflowDataService)
        }
        if ( this.map?.googleMap) {
            this.addLocationHandler = new AddLocationHandler(this.map.googleMap,this.messageService,this.dialogService, this.loadflowDataService)
        }
    }

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

    curZoom: number | undefined = 0
    zoomTextThreshold: number = 8
    canShowMarkers: boolean = false


    centerChanged() {
    }

    private readonly QB_COLOUR = '#7E4444'
    private readonly LOC_COLOUR = '#aaa'
    private readonly QB_SEL_COLOUR = 'red'
    private readonly LOC_SEL_COLOUR = 'white'
    private readonly LOC_EMPTY_COLOUR = 'white'
    private readonly BOUNDARY_COLOUR = '#00FF2F'

    locMarkerOptions: MapOptions<google.maps.marker.AdvancedMarkerElementOptions> = new MapOptions()
    linkOptions: MapOptions<google.maps.PolylineOptions> = new MapOptions()
    selectedLocMarker: MapAdvancedMarker | null = null
    selectedItem: SelectedMapItem = { location: null, link: null }
    selectedLinkLine: MapPolyline | null = null
    private selectObject(selectedItem: SelectedMapItem) {
        // de-select existing location marker 
        if (this.selectedLocMarker && this.selectedItem.location) {
            this.selectMarker(this.selectedItem.location, this.selectedLocMarker, false);
            this.selectedLocMarker = null;
        }
        // and link
        if (this.selectedLinkLine && this.selectedItem.link) {
            this.selectLink(this.selectedItem.link, this.selectedLinkLine, false);
            this.selectedLinkLine = null;
        }
        if (selectedItem.location && this.locMapMarkers) {
            let index = this.locMarkerOptions.getIndex(selectedItem.location.id)
            let mm = this.locMapMarkers.get(index)
            if (mm) {
                this.selectMarker(selectedItem.location, mm, true)
                this.selectedLocMarker = mm
                this.selectedItem = selectedItem
            }
        } else if (selectedItem.link && this.linkMapPolylines) {
            let index = this.linkOptions.getIndex(selectedItem.link.id)
            let mpl = this.linkMapPolylines.get(index);
            if (mpl) {
                this.selectLink(selectedItem.link, mpl, true)
                this.selectedLinkLine = mpl
                this.selectedItem = selectedItem
            }
        }
    }

    getLocSvg(): any {
        if ( !this.locSvg ) {
            const parser = new DOMParser();
            // A marker with a custom inline SVG.
            const pinSvgString = `<svg xmlns="http://www.w3.org/2000/svg" width="10" height="10"><circle cx="5" cy="5" r="4" /></svg>`
            this.locSvg = parser.parseFromString(pinSvgString, 'image/svg+xml').documentElement; 
            this.locSvg.style.setProperty('stroke','black')
            this.locSvg.style.setProperty('transform','translate(0px, 5px)')
        }
        let locSvg = this.locSvg.cloneNode(true)
        return locSvg;
    }

    markerTrackByFcn(index:number , mo: IMapData<google.maps.MarkerOptions>):any {
        return mo.id;
    }

    polylineTrackByFcn(index:number , mo: IMapData<google.maps.PolylineOptions>):any {
        return mo.id;
    }

    updateMapData(updateLocationData: UpdateLocationData) {

        if ( updateLocationData.clearBeforeUpdate ) {
            this.locMarkerOptions.clear()
            this.linkOptions.clear()    
        }
          
        let updateLocs = updateLocationData.updateLocations
        let deleteLocs = updateLocationData.deleteLocations
        let updateLinks = updateLocationData.updateLinks
        let deleteLinks = updateLocationData.deleteLinks

        // Locations
        // remove markers
        for (let loc of deleteLocs) {
            this.locMarkerOptions.remove(loc.id)
        }
        // replace or add markers as needed
        updateLocs.forEach(loc => {
            let markerOption = this.locMarkerOptions.get(loc.id);
            let options = this.getLocMarkerOptions(loc)
            if (markerOption) {
                markerOption.options = options
            } else {
                this.locMarkerOptions.add(loc.id, options)
            }
        })

        // Links
        // delete ones not needed
        for (let link of deleteLinks) {
            this.linkOptions.remove(link.id)
            // if its the selected one then remove the info window
            if ( link.id == this.loadflowDataService.selectedMapItem?.link?.id) {
                this.linkInfoWindow?.close()
            }
        }
        // replace or add branch options as needed
        updateLinks.forEach(link => {
            let branchOption = this.linkOptions.get(link.id);
            let options = this.getBranchOptions(link)
            if (branchOption) {
                branchOption.options = options
            } else {
                this.linkOptions.add(link.id, options)
            }
        })

    }

    getBranchOptions(b: ILoadflowLink):google.maps.PolylineOptions  {
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
            lineSymbol.strokeOpacity = this.isTripped(link) ? 0.15: 0.5
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
        return options;
    }

    private isTripped(link:ILoadflowLink): boolean {
        return link.branches.find(m=>this.loadflowDataService.isTripped(m.id)) ? true : false
    }

    getLocMarkerOptions(loc: ILoadflowLocation): google.maps.marker.AdvancedMarkerElementOptions {
        let fillColor = loc.isQB ? this.QB_COLOUR : this.LOC_COLOUR
        let fillOpacity = loc.hasNodes ? 1 : 0
        let locSvg = this.getLocSvg();

        locSvg.style.setProperty('opacity',fillOpacity.toFixed(1))
        locSvg.style.setProperty('fill',fillColor)

        return {
            position: {
                lat: loc.gisData.latitude,
                lng: loc.gisData.longitude,
            },
            title: `${loc.reference}: ${loc.name}`,
            content: locSvg,            
            zIndex: 15,
            gmpDraggable: true
        }
    }

    locMarkerClicked(mapData: IMapData<google.maps.marker.AdvancedMarkerElementOptions>) {
        if ( this.addBranchHandler?.inProgress ) {
            this.addBranchHandler.location2Selected(mapData.id)
        } else {
            this.loadflowDataService.selectLocation(mapData.id)
        }
    }

    locMarkerDragEnd(e: {mo: IMapData<google.maps.marker.AdvancedMarkerElementOptions>, e: any}) {
        let loc = this.loadflowDataService.getLocation(e.mo.id)
        if ( loc && this.datasetsService.currentDataset) {
            let data = { latitude: e.e.latLng.lat(), longitude: e.e.latLng.lng() };
            this.dataService.EditItem({id: loc.id, datasetId: this.datasetsService.currentDataset.id, className: "GridSubstationLocation", data: data }, (resp)=>{
                this.loadflowDataService.afterEdit(resp)
            }, (errors)=>{
                console.log(errors)
            })
        }
    }

    linkLineClicked(branchId: number) {
        this.loadflowDataService.selectLink(branchId)
    }

    selectMarker(loc: ILoadflowLocation, mm: MapAdvancedMarker, select: boolean) {
        //
        /*let s: any = mm.marker?.getIcon()
        if (loc.isQB) {
            s.fillColor = select ? this.QB_SEL_COLOUR : this.QB_COLOUR
        } else {
            s.fillColor = select ? this.LOC_SEL_COLOUR : this.LOC_COLOUR
        }
        s.strokeOpacity = select ? 1 : 0.5
        mm.marker?.setIcon(s)*/
        //
        this.showLocInfoWindow(loc, mm, select)
    }

    showLocInfoWindow(loc: ILoadflowLocation, mm: MapAdvancedMarker, select: boolean) {
        // open info window
        if (this.locInfoWindow) {
            if (select) {
                let center = { lat: loc.gisData.latitude, lng: loc.gisData.longitude }
                this.panTo(center, 7, () => {
                    //??
                    //?? Need to use dummy marker to set position since we are using an old version of angular/google-maps
                    //?? if/when upgrade to latest version should be able to use .open with the supplied MapAdvancedMarker
                    //??
                    if ( this.locInfoWindow && this.dummyMarker && this.dummyMarker.marker ) {
                        this.dummyMarker.marker.setPosition( mm.advancedMarker.position )
                        this.locInfoWindow.open(this.dummyMarker)
                    }    
                })
            } else {
                this.locInfoWindow.close()
            }
        }
    }

    selectLink(link: ILoadflowLink, mpl: MapPolyline, select: boolean) {
        //
        let isBoundaryLink = this.loadflowDataService.boundaryLinks.find(m=>m.id == link.id) ? true : false
        //
        let options = this.getPolylineOptions(link, select, isBoundaryLink)
        mpl.polyline?.setOptions(options);
        //
        this.showLinkInfoWindow(link, mpl, select)
    }

    showLinkInfoWindow(link: ILoadflowLink, mpl: MapPolyline, select: boolean) {
        // pan/zoom to new position
        if (select) {
            let center = {
                lat: (link.gisData1.latitude + link.gisData2.latitude) / 2,
                lng: (link.gisData1.longitude + link.gisData2.longitude) / 2
            }
            this.panTo(center, 7, () => {
                if ( this.linkInfoWindow) {
                    this.linkInfoWindow.position = center
                    this.linkInfoWindow.open()        
                }
            })
        } else {
            this.linkInfoWindow?.close()
        }
    }

    mapClick(e: google.maps.MapMouseEvent) {
        if ( this.addLocationHandler?.inProgress && e.latLng) {
            this.addLocationHandler.addLocation(e.latLng);
        } else {
            this.loadflowDataService.clearMapSelection();
        }
    }

    zoomChanged() {
        let curZoom = this.map?.googleMap?.getZoom()
    }

    panToBounds(bounds: google.maps.LatLngBounds) {
        this.map?.panToBounds(bounds);
    }

    panTo(center: google.maps.LatLngLiteral, minZoom: number, onIdle: (()=>void)) {

        this.panToCenter(center, () => {
            this.zoomTo(minZoom,onIdle)
        });

    }

    panToCenter(center: google.maps.LatLngLiteral,  onIdle: (()=>void)) {
        // Add an event listener for the 'idle' event
        if ( this.map?.googleMap) {
            google.maps.event.addListenerOnce(this.map?.googleMap, "idle", () => {
                // Place any code here that you want to execute after panning.
                onIdle()
            });    
        }
        this.map?.googleMap?.panTo(center)
    }

    zoomTo(minZoom: number,onIdle: (()=>void)) {
        let curZoom = this.map?.googleMap?.getZoom()
        if (curZoom && curZoom < minZoom) {
            if ( this.map?.googleMap) {
                google.maps.event.addListenerOnce(this.map?.googleMap, "idle", () => {
                    // Place any code here that you want to execute after panning.
                    onIdle()
                });    
            }
            this.map?.googleMap?.setZoom(minZoom)
        } else {
            onIdle()
        }
    }

    get currentZoom(): number {
        let zoom = this.map?.googleMap?.getZoom();
        if (zoom) {
            return zoom;
        } else {
            return 0;
        }
    }

    clearSelection() {
        this.loadflowDataService.clearMapSelection()
    }

    zoomIn() {
        let zoom = this.map?.googleMap?.getZoom();
        if (zoom) {
            this.map?.googleMap?.setZoom(zoom + 1);
        }
    }

    zoomOut() {
        let zoom = this.map?.googleMap?.getZoom();
        if (zoom) {
            this.map?.googleMap?.setZoom(zoom - 1);
        }
    }

    resetZoom() {
        this.map?.googleMap?.setZoom(this.zoom)
        this.map?.googleMap?.panTo(this.center)
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
            var index = this.linkOptions.getIndex(branch.id);
            if (index >= 0 && this.linkMapPolylines) {
                let mapPolyline = this.linkMapPolylines.get(index);
                if (mapPolyline) {
                    let options = this.getPolylineOptions(branch, false, true)
                    mapPolyline.polyline?.setOptions(options);
                    this.boundaryMapPolylines.set(branch.id, mapPolyline)
                }
            }
        })

    }

    newBranch(e: any) {
        this.addBranchHandler?.start(this.selectedItem.location)
    }

    addLocation(e: any) {
        this.addLocationHandler?.start()
    }

}

export class AddBranchHandler {
    constructor(private locInfoWindow: MapInfoWindow, 
        private messageService:ShowMessageService, 
        private dialogService:DialogService, 
        private loadflowDataService: LoadflowDataService) {
        }

    inProgress = false
    startLocation: ILoadflowLocation | null = null
    eventListener: google.maps.MapsEventListener | undefined

    start(loc: ILoadflowLocation | null) {        
        this.inProgress = true
        this.startLocation = loc
        this.messageService.showMessage("Please select location to connect to ... \n\n(click ESC to cancel)");
        this.locInfoWindow.close()
        let addBranchHandler = this
        this.eventListener = google.maps.event.addDomListener(document, 'keydown', (e: any)=>{
            if ( e.keyCode == 27 && addBranchHandler.inProgress) {
                addBranchHandler.cancel()
            }    
        });
    }

    cancel() {
        this.inProgress = false
        this.messageService.clearMessage();
        this.locInfoWindow.open()
        if ( this.eventListener ) {
            google.maps.event.removeListener(this.eventListener)
        }
    }

    location2Selected(locId: number) {
        let loc2 = this.loadflowDataService.getLocation(locId)
        if ( loc2 && this.startLocation) {
            this.messageService.clearMessage();
            let itemData = new NewItemData({node1: this.startLocation.reference, node2: loc2.reference})                
            this.dialogService.showLoadflowBranchDialog({ branch: itemData, ctrl: undefined}, ()=>{
                this.cancel()
            });
        } else {
            this.cancel()
        }
    }
}

export class AddLocationHandler {
    constructor( 
        private map: google.maps.Map,
        private messageService:ShowMessageService, 
        private dialogService:DialogService, 
        private loadflowDataService: LoadflowDataService) {
        }

    inProgress = false
    eventListener: google.maps.MapsEventListener | undefined

    start() {        
        this.inProgress = true
        this.messageService.showMessage("Please select location on the map ... \n\n(click ESC to cancel)");
        this.map.setOptions({gestureHandling:  'none'})
        let addLocationHandler = this
        this.eventListener = google.maps.event.addDomListener(document, 'keydown', (e: any)=>{
            if ( e.keyCode == 27 && addLocationHandler.inProgress) {
                addLocationHandler.cancel()
            }    
        });
    }

    cancel() {
        this.inProgress = false
        this.messageService.clearMessage();
        this.map.setOptions({gestureHandling:  'greedy'})
        if ( this.eventListener ) {
            google.maps.event.removeListener(this.eventListener)
        }
    }

    addLocation(latLng: google.maps.LatLng) {
        this.messageService.clearMessage()
        let itemData = new NewItemData({lat: latLng.lat(), lng: latLng.lng()})
        this.dialogService.showLoadflowLocationDialog(itemData, ()=>{
            this.cancel()
        });
    }

}

