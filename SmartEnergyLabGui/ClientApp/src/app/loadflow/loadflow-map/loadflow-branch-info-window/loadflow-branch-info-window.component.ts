import { Component } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService, LoadflowLink } from '../../loadflow-data-service.service';
import { Branch } from 'src/app/data/app.data';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
  selector: 'app-loadflow-branch-info-window',
  templateUrl: './loadflow-branch-info-window.component.html',
  styleUrls: ['./loadflow-branch-info-window.component.css']
})
export class LoadflowBranchInfoWindowComponent extends ComponentBase {
    constructor(
        private dataService: LoadflowDataService,
        private dialogService: DialogService,
        private datasetsService: DatasetsService
    ) {        
        super()
        this.addSub( this.dataService.ObjectSelected.subscribe( (selectedMapItem)=>{
            this.link = selectedMapItem.link
            this.filterData()
        }))
        this.addSub(this.dataService.NetworkDataLoaded.subscribe( ()=>{
            this.filterData()
        }))
        this.addSub(this.dataService.ResultsLoaded.subscribe( ()=>{
            this.filterData()
        }))
    }

    private link: LoadflowLink | null = null

    get totalFlowStr(): string {
        let tf = ''
        if ( this.dataService.selectedMapItem?.link) {
            if ( this.dataService.selectedMapItem?.link.totalFlow!=null ) {
                tf = this.dataService.selectedMapItem?.link.totalFlow.toFixed(0)
            }
        } 
        return tf
    }

    get totalFreeStr(): string {
        let tf = ''
        if ( this.dataService.selectedMapItem?.link) {
            if ( this.dataService.selectedMapItem?.link.totalFree!=null ) {
                tf = this.dataService.selectedMapItem?.link.totalFree.toFixed(0)
            }
        } 
        return tf
    }

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
        let itemData = this.dataService.getBranchEditorData(b.id)
        this.dialogService.showLoadflowBranchDialog(itemData)
    }

    filterData() {
        if ( this.link && this.dataService.branches) {
            let node1LocationId = this.link.node1LocationId
            let node2LocationId = this.link.node2LocationId
            this._branches = this.dataService.branches.data.filter( m=>m.node1LocationId>=0 && m.node2LocationId>=0 && 
                ( m.node1LocationId===node1LocationId && m.node2LocationId===node2LocationId) ||
                ( m.node1LocationId===node2LocationId && m.node2LocationId===node1LocationId))
        }
    }

    isTripped(branchId: number) {
        return this.dataService.isTripped(branchId)
    }

    toggleTrip(e: any, branchId: number) {
        if ( e.target.checked ) {
            this.dataService.addTrip(branchId)
        } else {
            this.dataService.removeTrip(branchId)
        }
    }

    get canTrip():boolean {
        return this.dataService.boundaryName ? true : false
    }

}
