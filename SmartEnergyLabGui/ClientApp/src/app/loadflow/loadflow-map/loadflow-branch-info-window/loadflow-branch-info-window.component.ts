import { Component } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { Branch, ILoadflowLink } from 'src/app/data/app.data';
import { DatasetsService, EditItemData } from 'src/app/datasets/datasets.service';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
  selector: 'app-loadflow-branch-info-window',
  templateUrl: './loadflow-branch-info-window.component.html',
  styleUrls: ['./loadflow-branch-info-window.component.css']
})
export class LoadflowBranchInfoWindowComponent extends ComponentBase {
    constructor(
        private loadflowDataService: LoadflowDataService,
        private dialogService: DialogService,
        private datasetsService: DatasetsService
    ) {        
        super()
        this.addSub( this.loadflowDataService.ObjectSelected.subscribe( (selectedMapItem)=>{
            this.link = selectedMapItem.link
        }))
    }

    private link: ILoadflowLink | null = null

    get name():string {
        let name = "";
        if ( this.link && this.link.branches.length>0 ) {
            name = `${this.link?.branches[0].node1Name} <=> ${this.link?.branches[0].node2Name}`
        }
        return name
    }

    get branchNames():string[] {        
        return this.link ? this.link.branches.map(m=>`${m.displayName}`) : []
    }

    get branches():Branch[] {
        return this.link ? this.link.branches : []
    }

    get branchCount():number {
        return this.link ? this.link.branches.length : 0
    }

    edit(b: Branch) {
        let itemData = new EditItemData<Branch>(b, this.datasetsService)
        this.dialogService.showLoadflowBranchDialog(itemData)
    }

}
