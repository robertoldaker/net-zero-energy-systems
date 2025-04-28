import { Component } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService, LoadflowLink } from '../../loadflow-data-service.service';

@Component({
  selector: 'app-loadflow-map-key',
  templateUrl: './loadflow-map-key.component.html',
  styleUrls: ['./loadflow-map-key.component.css']
})
export class LoadflowMapKeyComponent  extends ComponentBase {
    constructor(private loadflowDataService: LoadflowDataService) {
        super()
    }

    get boundaryName() {
        return this.loadflowDataService.boundaryName
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
}
