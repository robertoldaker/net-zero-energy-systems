import { Component, OnInit } from '@angular/core';
import { MapPowerService } from '../map-power.service';

@Component({
    selector: 'app-area-info-window',
    templateUrl: './area-info-window.component.html',
    styleUrls: ['./area-info-window.component.css']
})
export class AreaInfoWindowComponent implements OnInit {

    constructor(public mapPowerService: MapPowerService) { }

    ngOnInit(): void {
    }

}
