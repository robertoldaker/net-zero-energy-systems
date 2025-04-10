import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { LoadflowMapComponent } from '../loadflow-map.component';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { LoadflowDataComponent } from '../../data/loadflow-data/loadflow-data.component';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-loadflow-map-buttons',
    templateUrl: './loadflow-map-buttons.component.html',
    styleUrls: ['./loadflow-map-buttons.component.css']
})
export class LoadflowMapButtonsComponent  extends ComponentBase implements OnInit {

    constructor(private mapComponent: LoadflowMapComponent,private dataComponent: LoadflowDataComponent, private loadflowService: LoadflowDataService) { 
        super()
    }

    ngOnInit(): void {
    }

    @Output()
    onAddLocation: EventEmitter<any> = new EventEmitter()
    addLocation() {
        this.onAddLocation.emit()
    }

    resetZoom() {
        this.loadflowService.clearMapSelection()
        this.mapComponent.resetZoom()
    }

    tableView() {
        this.dataComponent.toggleMap()
    }

    toggleLocationDragging() {
        let newValue = !this.loadflowService.locationDragging
        this.loadflowService.setLocationDragging(newValue)
    }

    get locationDragging():boolean {
        return this.loadflowService.locationDragging
    }

    get isEditable():boolean {
        return !this.loadflowService.dataset.isReadOnly
    }

}
