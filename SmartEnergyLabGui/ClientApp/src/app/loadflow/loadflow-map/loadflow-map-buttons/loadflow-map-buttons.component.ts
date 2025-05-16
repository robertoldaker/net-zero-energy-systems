import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { LoadflowMapComponent, MapFlowFilter } from '../loadflow-map.component';
import { LoadflowDataService, PercentCapacityThreshold } from '../../loadflow-data-service.service';
import { LoadflowDataComponent } from '../../data/loadflow-data/loadflow-data.component';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-loadflow-map-buttons',
    templateUrl: './loadflow-map-buttons.component.html',
    styleUrls: ['./loadflow-map-buttons.component.css']
})
export class LoadflowMapButtonsComponent  extends ComponentBase implements OnInit {

    constructor(
        private mapComponent: LoadflowMapComponent,
        private dataComponent: LoadflowDataComponent, 
        private dataService: LoadflowDataService) { 
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
        this.dataService.clearMapSelection()
        this.mapComponent.resetZoom()
    }

    tableView() {
        this.dataComponent.toggleMap()
    }

    toggleLocationDragging() {
        let newValue = !this.dataService.locationDragging
        this.dataService.setLocationDragging(newValue)
    }

    get locationDragging():boolean {
        return this.dataService.locationDragging
    }

    get isEditable():boolean {
        return !this.dataService.dataset.isReadOnly
    }

    get hasResults():boolean {
        let result =  this.dataService.loadFlowResults ? true : false
        return result
    }

    flowFilters:any[] = [
        { id: MapFlowFilter.All, text: "All flows", enabled: true },
        { id: MapFlowFilter.Boundary, text: "Boundary only", enabled: this.dataService.boundaryName },
        { id: MapFlowFilter.Warning, text: `Flows > ${LoadflowDataService.WarningFlowThreshold}% capacity`, enabled: true },
        { id: MapFlowFilter.Critical, text: `Flows > ${LoadflowDataService.CriticalFlowThreshold}% capacity`, enabled: true }
        ]

    get flowFilter(): MapFlowFilter {
        return this.mapComponent.flowFilter
    }

    isFlowFilterDisabled(id: MapFlowFilter) {
        if ( id == MapFlowFilter.Boundary && !this.dataService.boundaryName) {
            return true
        } else {
            return false
        }
    }

    get isFlowFilterSet(): boolean {
        return this.mapComponent.flowFilter!==MapFlowFilter.All
    }

    selectFlowFilterOption(e: any, f: { id:MapFlowFilter,text: string}) {
        this.mapComponent.setFlowFilter(f.id)
    }

}
