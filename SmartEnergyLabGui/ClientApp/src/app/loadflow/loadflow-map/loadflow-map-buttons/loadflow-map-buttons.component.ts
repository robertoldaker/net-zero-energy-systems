import { Component, EventEmitter, OnInit, Output } from '@angular/core';

@Component({
    selector: 'app-loadflow-map-buttons',
    templateUrl: './loadflow-map-buttons.component.html',
    styleUrls: ['./loadflow-map-buttons.component.css']
})
export class LoadflowMapButtonsComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
    }

    @Output()
    onAddLocation: EventEmitter<any> = new EventEmitter()
    addLocation() {
        this.onAddLocation.emit()
    }

}
