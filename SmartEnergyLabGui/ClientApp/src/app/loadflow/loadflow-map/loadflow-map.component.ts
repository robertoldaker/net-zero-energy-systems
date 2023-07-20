import { AfterViewInit, Component, OnInit, ViewChild, ViewChildren } from '@angular/core';
import { GoogleMap, MapMarker } from '@angular/google-maps';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { Node, GridSubstation, NodeWrapper, Branch, CtrlWrapper, LoadflowCtrlType } from 'src/app/data/app.data';

@Component({
  selector: 'app-loadflow-map',
  templateUrl: './loadflow-map.component.html',
  styleUrls: ['./loadflow-map.component.css']
})

export class LoadflowMapComponent extends ComponentBase implements OnInit, AfterViewInit {

    @ViewChild(GoogleMap, { static: false }) map: GoogleMap | undefined
    @ViewChildren('nodeMarkers', { read: MapMarker }) nodeMapMarkers: MapMarker[] | undefined


    constructor( private loadflowDataService: LoadflowDataService ) {
        super();

    }

    ngOnInit(): void {

    }

    ngAfterViewInit(): void {
        if ( this.map ) {
            console.log('after view init');
            this.curZoom = this.map.googleMap?.getZoom()
            this.addSub(this.loadflowDataService.NetworkDataLoaded.subscribe(()=>{
                // add markers and lines to represent loadflow nodes, branches and ctrls           
                this.addMapData()
            }))  
        }
    }

    zoom = 6
    center: google.maps.LatLngLiteral = {
        //lat: 52.561928, lng: -1.464854
        lat: 54.5255, lng: -1.464854
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
        scaleControl: true
    }

    curZoom: number | undefined = 0
    zoomTextThreshold : number = 8
    canShowMarkers: boolean = false
    zoomChanged() {
    }


    centerChanged() {

    }

    selectedMarker: MapMarker | null = null

    nodeMarkerOptions: { options: google.maps.MarkerOptions, id:number }[]=[]
    qbMarkerOptions: { options: google.maps.MarkerOptions, id:number }[]=[]
    branchOptions: { options: google.maps.PolylineOptions,  id: number}[]=[]
    hvdcLineOptions: { options: google.maps.PolylineOptions,  id: number}[]=[]
    addMapData() {
        this.nodeMarkerOptions = []
        this.loadflowDataService.networkData.nodes.forEach(nodeWrapper => {
            if ( nodeWrapper.obj.gisData) {
                this.addNodeMarker(nodeWrapper.obj)
            }
        })

        this.loadflowDataService.networkData.branches.forEach(branchWrapper => {
            let b = branchWrapper.obj;
            if ( b.node1.gisData && b.node2.gisData) {
                this.addBranch(b)
            }
        })

        let hvdcLinks = this.loadflowDataService.networkData.ctrls.filter(m=>m.obj.type == LoadflowCtrlType.HVDC);
        // add links to coutries on the other end of the links
        hvdcLinks.forEach(ctrlWrapper => {
            let c = ctrlWrapper.obj;
            if ( c.node1 && c.node2) {
                if ( c.node1.gisData && c.node2.gisData) {
                    this.addHVDCLink(ctrlWrapper)
                }    
            } else {
                console.log(`Node(s) undefined for HVDC ctrl [${c.code}], n1=[${c.node1}], n2=${c.node2}`)
            }
        })
        // add marker to show the QB
        this.loadflowDataService.networkData.ctrls.forEach(ctrlWrapper => {
            let c = ctrlWrapper.obj;
            if ( c.node1 && c.node2) {
                if ( c.node1.gisData && c.node2.gisData) {
                    this.addQBMarker(ctrlWrapper)
                }    
            } 
        })
    }

    addBranch(b: Branch) {
        let node1 = b.node1
        let node2 = b.node2
        //console.log(`add branch ${node1.name} to ${node2.name}`)
        if ( node1.gisData && node2.gisData) {
            this.branchOptions.push( {
                options: {
                    path: [
                        {lat: node1.gisData.latitude, lng: node1.gisData.longitude },
                        {lat: node2.gisData.latitude, lng: node2.gisData.longitude },        
                    ],
                    strokeColor: 'blue',
                    strokeWeight: 1
                },
                id: b.id
            })    
        }
    }

    addHVDCLink(cw: CtrlWrapper) {
        let c = cw.obj
        let node1 = c.node1
        let node2 = c.node2
        if ( node1.gisData && node2.gisData) {
            //console.log(`add ctrl [${c.code}] ${node1.name} to ${node2.name}`)
            this.hvdcLineOptions.push( {
                options: {
                    path: [
                        {lat: node1.gisData.latitude, lng: node1.gisData.longitude },
                        {lat: node2.gisData.latitude, lng: node2.gisData.longitude },        
                    ],
                    strokeColor: 'red',
                    strokeWeight: 1,

                },
                id: cw.obj.id
            })    
        } else {
            console.log(`missing GIS data for ctrl [${c.code}] ${node1.name} to ${node2.name}`)
        }
    }

    addNodeMarker(node: Node) {
        let icon = {
            url: "/assets/images/loadflowNode.png", // url
            scaledSize: new google.maps.Size(40, 40), // scaled size
            origin: new google.maps.Point(0, 0), // origin
            anchor: new google.maps.Point(20, 20), // anchor
        };

        if ( node.gisData ) {
            this.nodeMarkerOptions.push({ 
                options: { 
                    icon: icon,
                    position: {
                        lat: node.gisData.latitude,
                        lng: node.gisData.longitude,
                    },
                    title: node.name,
                    opacity: 1,
                    zIndex: 15
                }, 
                id: node.id
            } )    
        }
    }

    addQBMarker(cw: CtrlWrapper) {
        let node = cw.obj.node1
        let icon = {
            url: "/assets/images/loadflowQB.png", // url
            scaledSize: new google.maps.Size(40, 40), // scaled size
            origin: new google.maps.Point(0, 0), // origin
            anchor: new google.maps.Point(20, 20), // anchor
        };

        if ( node.gisData ) {
            this.qbMarkerOptions.push({ 
                options: { 
                    icon: icon,
                    position: {
                        lat: node.gisData.latitude,
                        lng: node.gisData.longitude,
                    },
                    title: node.name,
                    opacity: 1,
                    zIndex: 16
                }, 
                id: node.id
            } )    
        }
    }

    nodeMarkerClicked(id: number) {

    }

    hvdcMarkerClicked(id: number) {

    }

    branchLineClicked(id: number) {
        console.log(`branchline clicked ${id}`)
    }

    ctrlLineClicked(id: number) {
        console.log(`ctrlline clicked ${id}`)
    }

    mapClick(e: google.maps.MapMouseEvent) {
        console.log(`mapClick lat=${e.latLng?.lat()}, lng=${e.latLng?.lng()}`)
    }
}
