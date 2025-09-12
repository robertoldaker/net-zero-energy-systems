import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { BoundCalcMapComponent, MapFlowFilter } from '../boundcalc-map.component';
import { BoundCalcDataService, PercentCapacityThreshold } from '../../boundcalc-data-service.service';
import { BoundCalcDataComponent } from '../../data/boundcalc-data/boundcalc-data.component';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-boundcalc-map-buttons',
    templateUrl: './boundcalc-map-buttons.component.html',
    styleUrls: ['./boundcalc-map-buttons.component.css']
})
export class BoundCalcMapButtonsComponent  extends ComponentBase implements OnInit {

    constructor(
        private mapComponent: BoundCalcMapComponent,
        private dataComponent: BoundCalcDataComponent,
        private dataService: BoundCalcDataService) {
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
        return !this.dataService.dataset?.isReadOnly
    }

    get hasResults():boolean {
        let result =  this.dataService.loadFlowResults ? true : false
        return result
    }

    flowFilters:any[] = [
        { id: MapFlowFilter.All, text: "All flows", enabled: true },
        { id: MapFlowFilter.Boundary, text: "Boundary only", enabled: this.dataService.boundaryName },
        { id: MapFlowFilter.Warning, text: `Flows > ${BoundCalcDataService.WarningFlowThreshold}% capacity`, enabled: true },
        { id: MapFlowFilter.Critical, text: `Flows > ${BoundCalcDataService.CriticalFlowThreshold}% capacity`, enabled: true }
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

    get isFilterSet(): boolean {
        return this.filtersApplied!=0
    }

    get filtersApplied(): number {
        return this.mapComponent.filtersApplied + (this.dataService.showFlowsAsPercent ? 1: 0)
    }

    selectFlowFilterOption(e: any, f: { id:MapFlowFilter,text: string}) {
        this.mapComponent.setFlowFilter(f.id)
    }

    get showFlowsAsPercent():boolean {
        return this.dataService.showFlowsAsPercent
    }

    showFlowsChecked(e: any, showFlowsAsPercent: boolean) {
        this.dataService.showFlowsAsPercent = showFlowsAsPercent
    }

}
