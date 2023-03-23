import { Component, OnInit } from '@angular/core';
import { MapPowerService } from '../map-power.service';

@Component({
    selector: 'app-gsp-info-window',
    templateUrl: './gsp-info-window.component.html',
    styleUrls: ['./gsp-info-window.component.css']
})
export class GspInfoWindowComponent implements OnInit {

    constructor(public mapPowerService: MapPowerService) { }

    ngOnInit(): void {
    }

}
