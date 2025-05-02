import { Component } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService, LoadflowLink } from '../../loadflow-data-service.service';
import { LinkLineData } from '../link-line-data';
import { LoadflowDataComponent } from '../../data/loadflow-data/loadflow-data.component';

@Component({
  selector: 'app-loadflow-map-key',
  templateUrl: './loadflow-map-key.component.html',
  styleUrls: ['./loadflow-map-key.component.css']
})
export class LoadflowMapKeyComponent  extends ComponentBase {
    constructor(private loadflowDataService: LoadflowDataService, private dataComponent: LoadflowDataComponent) {
        super()
    }

    get boundaryName() {
        return this.loadflowDataService.boundaryName
    }

    get boundaryTrip() {
        if ( this.loadflowDataService.boundaryTrip ) {
            return this.loadflowDataService.boundaryTrip.text
        } else if ( this.loadflowDataService.boundaryTrip == null ) {
            return "Intact"
        } else {
            return ""
        }
    }

    get boundaryTripBranchCodes():string[] {
        if ( this.loadflowDataService.boundaryTrip ) {
            return this.loadflowDataService.boundaryTrip.branchCodes
        } else {
            return []
        }
    }

    get warningFlowThreshold() {
        return LoadflowDataService.WarningFlowThreshold
    }

    get criticalFlowThreshold() {
        return LoadflowDataService.CriticalFlowThreshold
    }

    get hasResults():boolean {
        return this.loadflowDataService.loadFlowResults ? true : false
    }

    get boundaryStyle():object {
        return {stroke: LinkLineData.BOUNDARY_COLOUR}
    }

    selectBranch(branchCode: string) {
        this.dataComponent.showBranchOnMapByCode(branchCode)
    }
}
