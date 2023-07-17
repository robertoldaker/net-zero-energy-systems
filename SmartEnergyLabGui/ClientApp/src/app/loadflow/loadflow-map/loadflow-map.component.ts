import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { GoogleMap } from '@angular/google-maps';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
  selector: 'app-loadflow-map',
  templateUrl: './loadflow-map.component.html',
  styleUrls: ['./loadflow-map.component.css']
})

export class LoadflowMapComponent extends ComponentBase implements OnInit, AfterViewInit {

    @ViewChild(GoogleMap, { static: false }) map: GoogleMap | undefined

    ngOnInit(): void {

    }

    ngAfterViewInit(): void {
        if ( this.map ) {
            console.log('reading data');
            let map = this.map;
            this.map.data.loadGeoJson('/assets/geojson/Substations.geojson', {}, (df)=>{
                console.log('read data substations')
                if ( df ) {
                    for( let i=0;i<df.length;i++) {
                        let title = df[i].getProperty("Substation");
                        map.data.overrideStyle(df[i],{title: title, label: title, fillColor: 'red'});
                    }
                }
            });
            //
            this.map.data.loadGeoJson('/assets/geojson/ohl.geojson', {}, (df)=>{
                console.log('read data ohl')
            });
            //
            map.data.addListener('click', function(e: any) {
                map.data.overrideStyle(e.feature, {fillColor: 'red'});
                let ss = e.feature.getProperty("Substation")
                console.log(ss)
             })
        }
    }




    zoom = 6
    center: google.maps.LatLngLiteral = {
        lat: 52.561928, lng: -1.464854
    }
    options: google.maps.MapOptions = {            
        disableDoubleClickZoom: true,
        mapTypeId: 'roadmap',
        minZoom: 5,
        styles: [{ featureType: "poi", stylers: [{ visibility: "off" }] }, { stylers: [{ gamma: 5 }] }],
        mapTypeControl: false,
        scaleControl: true
    }

    zoomChanged() {

    }

    centerChanged() {

    }
}
