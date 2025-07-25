import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { GoogleMap } from '@angular/google-maps';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
  selector: 'app-admin-test',
  templateUrl: './admin-test.component.html',
  styleUrls: ['./admin-test.component.css']
})

export class AdminTestComponent extends ComponentBase implements OnInit, AfterViewInit {

    constructor(private messageService: ShowMessageService) {
        super();
    }

    @ViewChild(GoogleMap, { static: false }) map: GoogleMap | undefined
    @ViewChild('key') key: ElementRef | undefined


    ngOnInit(): void {

    }

    ngAfterViewInit(): void {
        if ( this.map ) {
            if ( this.key ) {
                this.map?.controls[google.maps.ControlPosition.TOP_LEFT].push(this.key.nativeElement);
            }
            let map = this.map;
            this.map.data.loadGeoJson('/assets/geojson/Substations.geojson', {}, (df)=>{
                if ( df ) {
                    for( let i=0;i<df.length;i++) {
                        let title = df[i].getProperty("Substation");
                        //map.data.overrideStyle(df[i],{title: title, label: title, fillColor: 'red'});
                    }
                }
            });
            //
            this.map.data.loadGeoJson('/assets/geojson/ohl.geojson', {}, (df)=>{
            });
            //
            map.data.addListener('click', function(e: any) {
                map.data.overrideStyle(e.feature, {fillColor: 'red'});
                let ss = e.feature.getProperty("Substation")
             })
        }
    }




    zoom = 7
    center: google.maps.LatLngLiteral = {
        lat: 52.561928, lng: -1.464854
    }
    options: google.maps.MapOptions = {
        disableDoubleClickZoom: true,
        mapTypeId: 'roadmap',
        minZoom: 3,
        //styles: [{ featureType: "poi", stylers: [{ visibility: "off" }] }, { stylers: [{ gamma: 1 }] }],
        styles: [
            { featureType: "poi", stylers: [{ visibility: "off" }] },
            { featureType: "road", stylers: [{ visibility: "off" }] },
            { featureType: "landscape", stylers: [{ visibility: "off" }] },
            { featureType: "administrative", stylers: [{ visibility: "off" }]}],
        mapTypeControl: false,
        scaleControl: true
    }

    zoomChanged() {
    }

    centerChanged() {

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

    isModal = true
    isError = true
    canClose = true
    withTimeout = false
    showMsgBox() {
        if ( this.withTimeout ) {
            this.messageService.showMessageWithTimeout('message with timeout')
        } else {
            if (this.isModal) {
                if (this.isError) {
                    this.messageService.showModalErrorMessage('This is a modal error message', this.canClose)
                } else {
                    this.messageService.showModalMessage('This is a modal message', this.canClose)
                }
            } else {
                if (this.isError) {
                    this.messageService.showErrorMessage('This is an error message', this.canClose)
                } else {
                    this.messageService.showMessage('This is a message', this.canClose)
                }
            }
        }
    }
}
