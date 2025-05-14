import { AfterViewInit, Component, ElementRef, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { GoogleMap, MapInfoWindow, MapPolyline } from '@angular/google-maps';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService, LoadflowLink, LoadflowLocation, PercentCapacityThreshold, SelectedMapItem, UpdateLocationData } from '../loadflow-data-service.service';
import { IMapData } from 'src/app/utils/map-options';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DatasetsService, NewItemData } from 'src/app/datasets/datasets.service';
import { DataClientService } from 'src/app/data/data-client.service';
import { MapAdvancedMarker } from './map-advanced-marker/map-advanced-marker';
import { LinkLabelData } from './link-label-data';
import { LocMarkerData } from './loc-marker-data';
import { LinkLineData } from './link-line-data';

@Component({
    selector: 'app-loadflow-map',
    templateUrl: './loadflow-map.component.html',
    styleUrls: ['./loadflow-map.component.css']
})
export class LoadflowMapComponent extends ComponentBase implements OnInit, AfterViewInit {

    @ViewChild(GoogleMap, { static: false }) map: GoogleMap | undefined
    @ViewChildren('locMarkers', { read: MapAdvancedMarker }) locMapMarkers: QueryList<MapAdvancedMarker> | undefined
    @ViewChildren('linkLines', { read: MapPolyline }) linkMapPolylines: QueryList<MapPolyline> | undefined
    @ViewChildren('linkMarkers', { read: MapAdvancedMarker }) linkMarkers: QueryList<MapAdvancedMarker> | undefined
    @ViewChild('key') key: ElementRef | undefined
    @ViewChild('buttons') buttons: ElementRef | undefined
    @ViewChild('locInfoWindow', { read: MapInfoWindow }) locInfoWindow: MapInfoWindow | undefined
    @ViewChild('linkInfoWindow', { read: MapInfoWindow }) linkInfoWindow: MapInfoWindow | undefined
    @ViewChild('divContainer') divContainer: ElementRef | undefined

    constructor(
        public loadflowDataService: LoadflowDataService,
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
            // update links to show boundary links
            this.linkPolylineData.updateAll()
        }))
        this.addSub(this.loadflowDataService.BoundaryTripSelected.subscribe((boundaryLinks) => {
            // update links to show boundary links
            this.linkPolylineData.updateAll()
        }))
        this.addSub(this.loadflowDataService.ObjectSelected.subscribe((selectedItem) => {
            // select an object (link or location)
            this.selectObject(selectedItem)
        }))
        this.addSub(loadflowDataService.LocationDraggingChanged.subscribe((value) => {
            this.updateDraggable()
        }))

    }

    flowFilter: PercentCapacityThreshold = PercentCapacityThreshold.OK
    addBranchHandler: AddBranchHandler | undefined
    addLocationHandler: AddLocationHandler | undefined

    ngOnInit(): void {

    }

    ngAfterViewInit(): void {
        if (this.key) {
            this.map?.controls[google.maps.ControlPosition.TOP_RIGHT].push(this.key.nativeElement);
        }
        if (this.buttons) {
            this.map?.controls[google.maps.ControlPosition.TOP_LEFT].push(this.buttons.nativeElement);
        }
        if (this.locInfoWindow) {
            this.addBranchHandler = new AddBranchHandler(this.locInfoWindow, this.messageService, this.dialogService, this.loadflowDataService)
        }
        if (this.map?.googleMap) {
            this.addLocationHandler = new AddLocationHandler(this.map.googleMap, this.messageService, this.dialogService, this.loadflowDataService)
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

    private readonly BOUNDARY_COLOUR = '#00FF2F'

    locMarkerData: LocMarkerData = new LocMarkerData(this)
    linkLabelData: LinkLabelData = new LinkLabelData(this)
    linkPolylineData: LinkLineData = new LinkLineData(this)
    selectedLocMarker: MapAdvancedMarker | null = null
    selectedItem: SelectedMapItem = new SelectedMapItem()
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
            let index = this.locMarkerData.getIndex(selectedItem.location.id)
            let mm = this.locMapMarkers.get(index)
            if (mm) {
                this.selectMarker(selectedItem.location, mm, true)
                this.selectedLocMarker = mm
                this.selectedItem = selectedItem
            }
        } else if (selectedItem.link && this.linkMapPolylines) {
            let index = this.linkPolylineData.getIndex(selectedItem.link.id)
            let mpl = this.linkMapPolylines.get(index);
            if (mpl) {
                this.selectLink(selectedItem.link, mpl, true)
                this.selectedLinkLine = mpl
                this.selectedItem = selectedItem
            }
        }
    }

    updateDraggable() {
        if (this.locMapMarkers) {
            for (let amm of this.locMapMarkers) {
                amm.advancedMarker.gmpDraggable = this.loadflowDataService.locationDragging
            }
        }
    }

    markerTrackByFcn(index: number, mo: IMapData<google.maps.marker.AdvancedMarkerElementOptions,LoadflowLocation>): any {
        return mo.id;
    }

    polylineTrackByFcn(index: number, mo: IMapData<google.maps.PolylineOptions,LoadflowLink>): any {
        return mo.id;
    }

    updateMapData(updateLocationData: UpdateLocationData) {

        this.locMarkerData.update(updateLocationData)
        this.linkLabelData.update(updateLocationData)
        this.linkPolylineData.update(updateLocationData)

    }

    locMarkerClicked(mapData: IMapData<google.maps.marker.AdvancedMarkerElementOptions,LoadflowLocation>) {
        if (this.addBranchHandler?.inProgress) {
            this.addBranchHandler.location2Selected(mapData.id)
        } else {
            this.loadflowDataService.selectLocation(mapData.id)
        }
    }

    locMarkerDragEnd(e: { mo: IMapData<google.maps.marker.AdvancedMarkerElementOptions,LoadflowLocation>, e: any }) {
        let loc = this.loadflowDataService.getLocation(e.mo.id)
        if (loc && this.datasetsService.currentDataset) {
            // stops polyLine and markers from being selected that meybe under the cursor after performing a marker drag
            e.e.domEvent.stopPropagation()
            // save data
            let data = { latitude: e.e.latLng.lat(), longitude: e.e.latLng.lng() };
            this.dataService.EditItem({ id: loc.id, datasetId: this.datasetsService.currentDataset.id, className: "GridSubstationLocation", data: data }, (resp) => {
                this.loadflowDataService.afterEdit(resp)
            }, (errors) => {
                console.log(errors)
            })
        }
    }

    linkLineClicked(branchId: number) {
        this.loadflowDataService.selectLink(branchId)
    }

    selectMarker(loc: LoadflowLocation, mm: MapAdvancedMarker, select: boolean) {
        //
        this.showLocInfoWindow(loc, mm, select)
    }

    showLocInfoWindow(loc: LoadflowLocation, mm: MapAdvancedMarker, select: boolean) {
        // open info window
        if (this.locInfoWindow) {
            if (select) {
                let center = { lat: loc.gisData.latitude, lng: loc.gisData.longitude }
                this.panTo(center, 7, () => {
                    if ( this.locInfoWindow && mm.advancedMarker.position) {
                        this.locInfoWindow.position = mm.advancedMarker.position
                        this.locInfoWindow.open()
                    }
                })
            } else {
                this.locInfoWindow.close()
            }
        }
    }

    selectLink(link: LoadflowLink, mpl: MapPolyline, select: boolean) {
        //
        this.showLinkInfoWindow(link, mpl, select)
    }

    showLinkInfoWindow(link: LoadflowLink, mpl: MapPolyline, select: boolean) {
        // pan/zoom to new position
        if (select) {
            let center = {
                lat: (link.gisData1.latitude + link.gisData2.latitude) / 2,
                lng: (link.gisData1.longitude + link.gisData2.longitude) / 2
            }
            this.panTo(center, 7, () => {
                if (this.linkInfoWindow) {
                    this.linkInfoWindow.position = center
                    this.linkInfoWindow.open()
                }
            })
        } else {
            this.linkInfoWindow?.close()
        }
    }

    mapClick(e: google.maps.MapMouseEvent) {
        if (this.addLocationHandler?.inProgress && e.latLng) {
            this.addLocationHandler.addLocation(e.latLng);
        } else {
            this.loadflowDataService.clearMapSelection();
        }
    }

    zoomChanged() {
        let curZoom = this.map?.googleMap?.getZoom()
        this.linkLabelData.updateForZoom()
    }

    redrawToggle: boolean = false
    redraw() {
        // This is needed since advanced map markers do not get updated when the map is hidden. polylines are ok.
        // this may not be needed when upgrading to latest angular/google-maps.
        // Getting the map to redraw fixes this but only way I could figure out is by panning a small amount
        if ( this.map?.googleMap) {
            let amount = this.redrawToggle ? 1 : -1
            // Add an event listener for the 'idle' event
            if (this.map?.googleMap) {
                google.maps.event.addListenerOnce(this.map?.googleMap, "idle", () => {
                    // Needed here since it needs the projection defined to work out when to show the labels
                    this.linkLabelData.updateForZoom()
                });
            }
            // only way to get the map to refresh after being hidden!!
            this.map.googleMap.panBy(0,amount)
            this.redrawToggle = !this.redrawToggle
        }
    }

    panToBounds(bounds: google.maps.LatLngBounds) {
        this.map?.panToBounds(bounds);
    }

    panTo(center: google.maps.LatLngLiteral, minZoom: number, onIdle: (() => void)) {

        this.panToCenter(center, () => {
            this.zoomTo(minZoom, onIdle)
        });

    }

    panToCenter(center: google.maps.LatLngLiteral, onIdle: (() => void)) {
        // Add an event listener for the 'idle' event
        if (this.map?.googleMap) {
            google.maps.event.addListenerOnce(this.map?.googleMap, "idle", () => {
                // Place any code here that you want to execute after panning.
                onIdle()
            });
        }
        this.map?.googleMap?.panTo(center)
    }

    zoomTo(minZoom: number, onIdle: (() => void)) {
        let curZoom = this.map?.googleMap?.getZoom()
        if (curZoom && curZoom < minZoom) {
            if (this.map?.googleMap) {
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

    get googleMap(): google.maps.Map | undefined {
        return this.map?.googleMap
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

    newBranch(e: any) {
        this.addBranchHandler?.start(this.selectedItem.location)
    }

    addLocation(e: any) {
        this.addLocationHandler?.start()
    }

    setFlowFilter( flowFilter: PercentCapacityThreshold) {
        this.flowFilter = flowFilter
        this.linkLabelData.updateForZoom()
    }

}

export class AddBranchHandler {
    constructor(private locInfoWindow: MapInfoWindow,
        private messageService: ShowMessageService,
        private dialogService: DialogService,
        private loadflowDataService: LoadflowDataService) {
    }

    inProgress = false
    startLocation: LoadflowLocation | null = null
    eventListener: google.maps.MapsEventListener | undefined

    start(loc: LoadflowLocation | null) {
        this.inProgress = true
        this.startLocation = loc
        this.messageService.showMessage("Please select location to connect to ... \n\n(click ESC to cancel)");
        this.locInfoWindow.close()
        let addBranchHandler = this
        this.eventListener = google.maps.event.addDomListener(document, 'keydown', (e: any) => {
            if (e.keyCode == 27 && addBranchHandler.inProgress) {
                addBranchHandler.cancel()
            }
        });
    }

    cancel() {
        this.inProgress = false
        this.messageService.clearMessage();
        this.locInfoWindow.open()
        if (this.eventListener) {
            google.maps.event.removeListener(this.eventListener)
        }
    }

    location2Selected(locId: number) {
        let loc2 = this.loadflowDataService.getLocation(locId)
        if (loc2 && this.startLocation) {
            this.messageService.clearMessage();
            let itemData = new NewItemData({ node1: this.startLocation.reference, node2: loc2.reference })
            this.dialogService.showLoadflowBranchDialog({ branch: itemData, ctrl: undefined }, () => {
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
        private messageService: ShowMessageService,
        private dialogService: DialogService,
        private loadflowDataService: LoadflowDataService) {
    }

    inProgress = false
    eventListener: google.maps.MapsEventListener | undefined

    start() {
        this.inProgress = true
        this.messageService.showMessage("Please select location on the map ... \n\n(click ESC to cancel)");
        this.map.setOptions({ gestureHandling: 'none' })
        let addLocationHandler = this
        this.eventListener = google.maps.event.addDomListener(document, 'keydown', (e: any) => {
            if (e.keyCode == 27 && addLocationHandler.inProgress) {
                addLocationHandler.cancel()
            }
        });
    }

    cancel() {
        this.inProgress = false
        this.messageService.clearMessage();
        this.map.setOptions({ gestureHandling: 'greedy' })
        if (this.eventListener) {
            google.maps.event.removeListener(this.eventListener)
        }
    }

    addLocation(latLng: google.maps.LatLng) {
        this.messageService.clearMessage()
        let itemData = new NewItemData({ lat: latLng.lat(), lng: latLng.lng() })
        this.dialogService.showLoadflowLocationDialog(itemData, () => {
            this.cancel()
        });
    }
}

