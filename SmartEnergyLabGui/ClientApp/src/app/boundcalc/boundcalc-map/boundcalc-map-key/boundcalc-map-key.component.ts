import { Component } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { BoundCalcDataService, BoundCalcLink } from '../../boundcalc-data-service.service';
import { LinkLineData } from '../link-line-data';
import { BoundCalcDataComponent } from '../../data/boundcalc-data/boundcalc-data.component';

@Component({
  selector: 'app-boundcalc-map-key',
  templateUrl: './boundcalc-map-key.component.html',
  styleUrls: ['./boundcalc-map-key.component.css']
})
export class BoundCalcMapKeyComponent  extends ComponentBase {
    constructor(private boundcalcDataService: BoundCalcDataService, private dataComponent: BoundCalcDataComponent) {
        super()
    }

    get boundaryName() {
        return this.boundcalcDataService.boundaryName
    }

    get boundaryTrip() {
        if ( this.boundcalcDataService.boundaryTripResult?.trip ) {
            return this.boundcalcDataService.boundaryTripResult.trip.text
        } else if ( this.boundcalcDataService.boundaryTripResult?.trip == null ) {
            return "Intact"
        } else {
            return ""
        }
    }

    get boundaryTripBranchCodes():string[] {
        if ( this.boundcalcDataService.boundaryTripResult?.trip ) {
            return this.boundcalcDataService.boundaryTripResult.trip.branchCodes
        } else {
            return []
        }
    }

    get boundaryCapacity() {
        if ( this.boundcalcDataService.boundaryTripResult ) {
            return this.boundcalcDataService.boundaryTripResult.capacity.toFixed(0)
        } else {
            return ''
        }
    }

    get warningFlowThreshold() {
        return BoundCalcDataService.WarningFlowThreshold
    }

    get criticalFlowThreshold() {
        return BoundCalcDataService.CriticalFlowThreshold
    }

    get hasResults():boolean {
        return this.boundcalcDataService.loadFlowResults ? true : false
    }

    get boundaryStyle():object {
        return {stroke: LinkLineData.BOUNDARY_COLOUR}
    }

    get flowUnits(): string {
        let units = this.boundcalcDataService.showFlowsAsPercent ? "%" : "MW"
        return units
    }

    selectBranch(branchCode: string) {
        this.dataComponent.showBranchOnMapByCode(branchCode)
    }
}
