import { Component } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { Branch, ILoadflowLink } from 'src/app/data/app.data';
import { DatasetsService } from 'src/app/datasets/datasets.service';
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
            this.filterData()
        }))
        this.addSub(this.loadflowDataService.NetworkDataLoaded.subscribe( (networkData)=>{
            this.filterData()
        }))
    }

    private link: ILoadflowLink | null = null
    private _branches:Branch[] = []

    get name():string {
        let name = "";
        if ( this.link && this._branches.length>0 ) {
            name = `${this._branches[0].node1Name} <=> ${this._branches[0].node2Name}`
        }
        return name
    }

    get branches():Branch[] {
        return this._branches;
    }

    get branchCount():number {
        return this._branches.length
    }

    edit(b: Branch) {
        let itemData = this.loadflowDataService.getBranchEditorData(b.id)
        this.dialogService.showLoadflowBranchDialog(itemData)
    }

    filterData() {
        if ( this.link ) {
            let node1LocationId = this.link.node1LocationId
            let node2LocationId = this.link.node2LocationId
            let branches = this.loadflowDataService.networkData.branches
            this._branches = branches.data.filter( m=>m.node1LocationId>=0 && m.node2LocationId>=0 && 
                ( m.node1LocationId===node1LocationId && m.node2LocationId===node2LocationId) ||
                ( m.node1LocationId===node2LocationId && m.node2LocationId===node1LocationId))
        }
    }

}
