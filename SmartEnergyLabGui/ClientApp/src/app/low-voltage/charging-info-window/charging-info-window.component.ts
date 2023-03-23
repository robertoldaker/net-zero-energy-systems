import { Component, OnInit } from '@angular/core';
import { MapEvService } from '../map-ev.service';

@Component({
    selector: 'app-charging-info-window',
    templateUrl: './charging-info-window.component.html',
    styleUrls: ['./charging-info-window.component.css']
})
export class ChargingInfoWindowComponent implements OnInit {

    constructor(public mapEvService: MapEvService) { }

    ngOnInit(): void {
    }

    edit() {

    }

}
