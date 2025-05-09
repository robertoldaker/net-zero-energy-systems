import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { LoadflowMapComponent } from '../loadflow-map.component';
import { LoadflowDataService, PercentCapacityThreshold } from '../../loadflow-data-service.service';
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

    get hasResults():boolean {
        let result =  this.loadflowService.loadFlowResults ? true : false
        return result
    }

    flowFilters = [
        { id: PercentCapacityThreshold.OK, text: "All flows" },
        { id: PercentCapacityThreshold.Warning, text: `Flows > ${LoadflowDataService.WarningFlowThreshold}% capacity` },
        { id: PercentCapacityThreshold.Critical, text: `Flows > ${LoadflowDataService.CriticalFlowThreshold}% capacity` }
    ]

    get flowFilter(): PercentCapacityThreshold {
        return this.mapComponent.flowFilter
    }

    get isFlowFilterSet(): boolean {
        return this.mapComponent.flowFilter!==PercentCapacityThreshold.OK
    }

    selectFlowFilterOption(e: any, f: { id:PercentCapacityThreshold,text: string}) {
        this.mapComponent.setFlowFilter(f.id)
    }

}
