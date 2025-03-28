import { Component } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';

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
}
