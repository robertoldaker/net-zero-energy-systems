import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { GoogleMap } from '@angular/google-maps';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
  selector: 'app-admin-test',
  templateUrl: './admin-test.component.html',
  styleUrls: ['./admin-test.component.css']
})

export class AdminTestComponent extends ComponentBase implements OnInit, AfterViewInit {

    @ViewChild(GoogleMap, { static: false }) map: GoogleMap | undefined

    ngOnInit(): void {

    }

    ngAfterViewInit(): void {
        if ( this.map ) {
            console.log('reading data');
            let map = this.map;
            this.map.data.loadGeoJson('/assets/geojson/Substations.geojson', {}, (df)=>{
                console.log('read data')
                if ( df ) {
                    for( let i=0;i<df.length;i++) {
                        let title = df[i].getProperty("Substation");
                        map.data.overrideStyle(df[i],{title: title, label: title, fillColor: 'red'});
                    }
                }
            });
            //
            map.data.addListener('click', function(e: any) {
                map.data.overrideStyle(e.feature, {fillColor: 'red'});
                let ss = e.feature.getProperty("Substation")
                console.log(ss)
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
        minZoom: 7,
        styles: [{ featureType: "poi", stylers: [{ visibility: "off" }] }, { stylers: [{ gamma: 5 }] }],
        mapTypeControl: false,
        scaleControl: true
    }

    zoomChanged() {

    }

    centerChanged() {

    }
}
