import { AfterViewInit, Component, ElementRef, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { GoogleMap, MapInfoWindow, MapMarker, MapPolyline } from '@angular/google-maps';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService, SelectedMapItem } from '../loadflow-data-service.service';
import { Node, GridSubstation, NodeWrapper, Branch, CtrlWrapper, LoadflowCtrlType, LoadflowLocation, LoadflowBranch, GISData, BoundaryTrips, BoundaryTrip, BoundaryTripType } from 'src/app/data/app.data';
import { MapOptions } from 'src/app/utils/map-options';

@Component({
  selector: 'app-loadflow-map',
  templateUrl: './loadflow-map.component.html',
  styleUrls: ['./loadflow-map.component.css']
})

export class LoadflowMapComponent extends ComponentBase implements OnInit, AfterViewInit {

    @ViewChild(GoogleMap, { static: false }) map: GoogleMap | undefined
    @ViewChildren('locMarkers', { read: MapMarker }) locMapMarkers: QueryList<MapMarker> | undefined
    @ViewChildren('branchLines', { read: MapPolyline }) branchMapPolylines: QueryList<MapPolyline> | undefined
    @ViewChild('key') key: ElementRef | undefined
    @ViewChild('locInfoWindow', { read: MapInfoWindow }) locInfoWindow: MapInfoWindow | undefined;
    @ViewChild('branchInfoWindow', { read: MapInfoWindow }) branchInfoWindow: MapInfoWindow | undefined;

    constructor( private loadflowDataService: LoadflowDataService ) {
        super();
        if (this.loadflowDataService.locationData.locations.length > 0) {
            this.addMapData();
        } 
        this.addSub(this.loadflowDataService.LocationDataLoaded.subscribe(()=>{
            // add markers and lines to represent loadflow nodes, branches and ctrls           
            this.addMapData()
        }))     
        this.addSub(this.loadflowDataService.ResultsLoaded.subscribe((loadflowResults)=>{
            this.selectBoundaryBranches(loadflowResults.boundaryTrips.trips)
        }))         
        this.addSub(this.loadflowDataService.ObjectSelected.subscribe((selectedItem)=>{
            this.selectObject(selectedItem)
        }))         
    }

    ngOnInit(): void {

    }

    ngAfterViewInit(): void {
        if ( this.key ) {
            this.map?.controls[google.maps.ControlPosition.TOP_LEFT].push(this.key.nativeElement);
        }
        if (this.loadflowDataService.loadFlowResults) {
            this.selectBoundaryBranches(this.loadflowDataService.loadFlowResults.boundaryTrips.trips);
        } 
    }

    zoom = 7
    center: google.maps.LatLngLiteral = {
        lat: 52.90829, lng: -0.97960
    }
    options: google.maps.MapOptions = {            
        disableDoubleClickZoom: true,
        mapTypeId: 'roadmap',
        minZoom: 3,
        styles: [
                { featureType: "poi", stylers: [{ visibility: "off" }] }, 
                { featureType: "road", stylers: [{ visibility: "off" }] }, 
                { featureType: "landscape", stylers: [{ visibility: "off" }] },
                { featureType: "administrative", stylers: [{ visibility: "off" }]}],
        mapTypeControl: false,
        fullscreenControl: false,
        streetViewControl: false,
        scaleControl: true
    }

    curZoom: number | undefined = 0
    zoomTextThreshold : number = 8
    canShowMarkers: boolean = false


    centerChanged() {
    }

    private readonly QB_COLOUR = '#7E4444'
    private readonly LOC_COLOUR = 'grey'
    private readonly QB_SEL_COLOUR = 'red'
    private readonly LOC_SEL_COLOUR = 'white'
    private readonly BOUNDARY_COLOUR = '#00FF2F'
    
    locMarkerOptions: MapOptions<google.maps.MarkerOptions> = new MapOptions()
    branchOptions: MapOptions<google.maps.PolylineOptions> = new MapOptions()
    selectedLocMarker: MapMarker | null = null
    selectedItem: SelectedMapItem  = { location: null, branch: null }
    selectedBranchLine: MapPolyline | null =null
    private selectObject(selectedItem: SelectedMapItem) {
        // de-select existing location marker 
        if ( this.selectedLocMarker && this.selectedItem.location ) {
            this.selectMarker(this.selectedItem.location, this.selectedLocMarker, false);
            this.selectedLocMarker = null;
        }
        // and branch
        if ( this.selectedBranchLine && this.selectedItem.branch ) {
            this.selectBranch(this.selectedItem.branch, this.selectedBranchLine, false);
            this.selectedBranchLine = null;
        }
        if ( selectedItem.location && this.locMapMarkers) {
            let index = this.locMarkerOptions.getIndex(selectedItem.location.id)
            let mm = this.locMapMarkers.get(index)
            if ( mm ) {
                this.selectMarker(selectedItem.location, mm,true)
                this.selectedLocMarker = mm
                this.selectedItem = selectedItem
            }
        } else if ( selectedItem.branch && this.branchMapPolylines) {
            let index = this.branchOptions.getIndex(selectedItem.branch.id)
            let mpl = this.branchMapPolylines.get(index);
            if ( mpl ) {
                this.selectBranch(selectedItem.branch, mpl,true)
                this.selectedBranchLine = mpl
                this.selectedItem = selectedItem
            }
        } 
    }
    
    addMapData() {
        this.locMarkerOptions.clear()
        this.loadflowDataService.locationData.locations.forEach(loc => {
            this.addLocMarker(loc)
        })
        this.branchOptions.clear();
        this.loadflowDataService.locationData.branches.forEach(branch => {
            this.addBranch(branch)
        })
    }

    addBranch(b: LoadflowBranch) {
        let options = this.getPolylineOptions(b,false,false);
        options.path =[
            {lat: b.gisData1.latitude, lng: b.gisData1.longitude },
            {lat: b.gisData2.latitude, lng: b.gisData2.longitude },        
        ]; 
        this.branchOptions.add(b.id, options)
    }

    private getPolylineOptions(b: LoadflowBranch, selected: boolean, isBoundary: boolean): google.maps.PolylineOptions {
        let options: google.maps.PolylineOptions = {
            strokeWeight: 1
        }
        let lineSymbol = this.getLineSymbol(selected, isBoundary)
        if ( b.branch.linkType == 'HVDC') {
            options.strokeColor = isBoundary ? this.BOUNDARY_COLOUR : 'black'
            options.strokeOpacity = 0 // makes it invisible
            options.strokeWeight = 20 // allows it to be selected when mouse close to line not directly over it
            options.icons = [
                {
                  icon: lineSymbol,
                  offset: "0",
                  repeat: selected || isBoundary ? "8px" : "4px",
                },
              ]
        } else {
            if ( isBoundary ) {
                options.strokeColor = this.BOUNDARY_COLOUR;
            } else  if ( b.voltage == 400) {
                options.strokeColor = 'blue'
            } else if ( b.voltage == 275) {
                options.strokeColor = 'red'
            } else {
                options.strokeColor = 'black';
            }
            options.strokeOpacity = 0 // makes it invisible
            options.strokeWeight = 20 // allows it to be selected when mouse close to line not directly over it
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

    private getLineSymbol(select: boolean, isBoundary: boolean): google.maps.Symbol {
        let s = {
            path: "M 0,-1,0,1", 
            strokeOpacity: select || isBoundary ? 1 : 0.5, 
            scale: isBoundary ? 3 : (select ? 2 : 1)
        }
        return s;
    }
    
    addLocMarker(loc: LoadflowLocation) {

        let fillColor = loc.isQB ? this.QB_COLOUR : this.LOC_COLOUR
        let sqIcon:google.maps.Symbol = {
            path: google.maps.SymbolPath.CIRCLE,
            scale: 4,
            strokeOpacity: 0,
            strokeColor: 'black',
            strokeWeight: 1,
            fillOpacity: 1,
            fillColor: fillColor
        };

        this.locMarkerOptions.add(loc.id,
            { 
                icon: sqIcon,
                position: {
                    lat: loc.gisData.latitude,
                    lng: loc.gisData.longitude,
                },
                title: loc.name,
                opacity: 1,
                zIndex: 15
            }
        )    
    }

    locMarkerClicked(locId: number) {
        this.loadflowDataService.selectLocation(locId)
    }

    branchLineClicked(branchId: number) {
        this.loadflowDataService.selectBranch(branchId)
    }

    selectMarker(loc: LoadflowLocation, mm: MapMarker, select: boolean) {
        //
        let s:any = mm.marker?.getIcon()
        if ( loc.isQB ) {
            s.fillColor = select ? this.QB_SEL_COLOUR : this.QB_COLOUR
        } else {
            s.fillColor = select ? this.LOC_SEL_COLOUR : this.LOC_COLOUR
        }
        s.strokeOpacity = select ? 1 : 0
        mm.marker?.setIcon(s)
        //
        this.showLocInfoWindow( loc, mm, select)
    }

    showLocInfoWindow(loc: LoadflowLocation, mm: MapMarker, select: boolean) {
        // pan/zoom to new position
        if ( select ) {
            let center = { lat: loc.gisData.latitude, lng: loc.gisData.longitude}
            this.panTo(center,7)    
        }
        // open info window
        if ( this.locInfoWindow) {
            if ( select ) {
                this.locInfoWindow.open(mm)
            } else {
                this.locInfoWindow.close()
            }
        }
    }

    selectBranch(branch: LoadflowBranch, mpl: MapPolyline, select: boolean) {
        //
        let options = this.getPolylineOptions(branch, false, select)
        mpl.polyline?.setOptions(options);
        //
        this.showBranchInfoWindow(branch, mpl, select)
    }

    showBranchInfoWindow(branch: LoadflowBranch, mpl: MapPolyline, select: boolean) {
        // pan/zoom to new position
        let center = { lat: (branch.gisData1.latitude + branch.gisData2.latitude)/2, 
                       lng: (branch.gisData1.longitude + branch.gisData2.longitude)/2}
        if ( select ) {
            this.panTo(center,7)
        }
        // open info window
        if ( this.branchInfoWindow) {
            if ( select ) {
                this.branchInfoWindow.position = center
                this.branchInfoWindow.open()    
            } else {
                this.branchInfoWindow.close()
            }
        }
    }

    mapClick(e: google.maps.MapMouseEvent) {
        console.log(`mapClick lat=${e.latLng?.lat()}, lng=${e.latLng?.lng()}`)
        this.loadflowDataService.clearMapSelection();
    }

    zoomChanged() {
        let curZoom = this.map?.googleMap?.getZoom()
        console.log(`zoom changed ${curZoom}`)
    }

    panToBounds(bounds: google.maps.LatLngBounds) {
        this.map?.panToBounds(bounds);
    }

    panTo(center: google.maps.LatLngLiteral, minZoom: number) {

        let curZoom = this.map?.googleMap?.getZoom()
        if ( curZoom && curZoom < minZoom ) {
            this.map?.googleMap?.setZoom(minZoom)
        }

        this.map?.googleMap?.setCenter(center)
        this.map?.googleMap?.panTo(center)
    }

    zoomTo(minZoom: number) {
        let curZoom = this.map?.googleMap?.getZoom()
        if ( curZoom && curZoom < minZoom ) {
            this.map?.googleMap?.setZoom(minZoom)
        }
    }

    get currentZoom():number {
        let zoom = this.map?.googleMap?.getZoom();
        if ( zoom ) {
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
        if ( zoom ) {
            this.map?.googleMap?.setZoom(zoom+1);
        }
    }

    zoomOut() {
        let zoom = this.map?.googleMap?.getZoom();
        if ( zoom ) {
            this.map?.googleMap?.setZoom(zoom-1);
        }
    }

    boundaryBranches: LoadflowBranch[] = []
    boundaryMapPolylines: Map<number,MapPolyline> = new Map()
    selectBoundaryBranches(boundaryTrips: BoundaryTrip[]) {
        // unselect current ones
        this.boundaryBranches.forEach((branch)=>{
            let mapPolyline = this.boundaryMapPolylines.get(branch.id)
            if ( mapPolyline ) {
                let options = this.getPolylineOptions(branch, false, false)
                mapPolyline.polyline?.setOptions(options);
            }
        })
        // select new ones
        this.boundaryBranches = []
        this.boundaryMapPolylines.clear()
        this.boundaryBranches = this.loadflowDataService.locationData.branches.
                filter(m=>boundaryTrips.find(n=>n.type==BoundaryTripType.Single && n.branchIds[0]==m.id));
        this.boundaryBranches.forEach( (branch)=>{
            var index = this.branchOptions.getIndex(branch.id);
            if ( index>=0 && this.branchMapPolylines) {
                let mapPolyline = this.branchMapPolylines.get(index);
                if ( mapPolyline ) {
                    let options = this.getPolylineOptions(branch, false, true)
                    mapPolyline.polyline?.setOptions(options);
                    this.boundaryMapPolylines.set(branch.id,mapPolyline)
                }
            }
        })

    }

}
