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
        if ( this.loadflowDataService.loadFlowResults) {
            this.boundaryName = this.loadflowDataService.boundaryName;
        }
        this.addSub(this.loadflowDataService.ResultsLoaded.subscribe(()=>{
            this.boundaryName = this.loadflowDataService.boundaryName;
        }))         
    }

    boundaryName: string | undefined
}
