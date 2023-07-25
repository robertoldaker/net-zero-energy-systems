import { Component } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { LoadflowBranch } from 'src/app/data/app.data';

@Component({
  selector: 'app-loadflow-branch-info-window',
  templateUrl: './loadflow-branch-info-window.component.html',
  styleUrls: ['./loadflow-branch-info-window.component.css']
})
export class LoadflowBranchInfoWindowComponent extends ComponentBase {
    constructor(private loadflowDataService: LoadflowDataService) {        
        super()
        this.addSub( this.loadflowDataService.ObjectSelected.subscribe( (selectedMapItem)=>{
            this.branch = selectedMapItem.branch
        }))
    }

    private branch: LoadflowBranch | null = null

    get name():string {
        return this.branch?.branch ? this.branch?.branch.code : ''
    }
    get linkType():string {
        return this.branch?.branch ? this.branch?.branch.linkType : ''
    }
}
