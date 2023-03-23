import { Component, OnInit } from '@angular/core';
import { MapPowerService } from '../map-power.service';

@Component({
    selector: 'app-primary-info-window',
    templateUrl: './primary-info-window.component.html',
    styleUrls: ['./primary-info-window.component.css']
})
export class PrimaryInfoWindowComponent implements OnInit {

    constructor(public mapPowerService: MapPowerService) {
    }

    ngOnInit(): void {
    }

}
