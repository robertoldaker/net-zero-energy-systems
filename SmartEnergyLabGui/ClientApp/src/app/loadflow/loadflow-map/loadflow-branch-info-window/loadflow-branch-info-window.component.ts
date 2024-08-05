import { Component } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { LoadflowLink } from 'src/app/data/app.data';

@Component({
  selector: 'app-loadflow-branch-info-window',
  templateUrl: './loadflow-branch-info-window.component.html',
  styleUrls: ['./loadflow-branch-info-window.component.css']
})
export class LoadflowBranchInfoWindowComponent extends ComponentBase {
    constructor(private loadflowDataService: LoadflowDataService) {        
        super()
        this.addSub( this.loadflowDataService.ObjectSelected.subscribe( (selectedMapItem)=>{
            this.link = selectedMapItem.link
        }))
    }

    private link: LoadflowLink | null = null

    get name():string {
        let name = `${this.link?.branches[0].node1Name} <=> ${this.link?.branches[0].node2Name}`
        return name
    }

    get branchNames():string[] {
        return this.link?.branches[0] ? this.link?.branches.map(m=>`${m.displayName}`) : []
    }

    get branchCount():number {
        return this.link?.branches[0] ? this.link?.branches.length : 0
    }

}
