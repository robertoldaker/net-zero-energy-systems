import { Component } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { BoundCalcDataService, BoundCalcLink } from '../../boundcalc-data-service.service';
import { Branch } from 'src/app/data/app.data';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
  selector: 'app-boundcalc-branch-info-window',
  templateUrl: './boundcalc-branch-info-window.component.html',
  styleUrls: ['./boundcalc-branch-info-window.component.css']
})
export class BoundCalcBranchInfoWindowComponent extends ComponentBase {
    constructor(
        private dataService: BoundCalcDataService,
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

    private link: BoundCalcLink | null = null

    private _branches:Branch[] = []
    private _deletedBranches:Branch[] = []

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

    get deletedBranches():Branch[] {
        return this._deletedBranches;
    }

    filterData() {
        if ( this.link && this.dataService.branches) {
            let node1LocationId = this.link.node1LocationId
            let node2LocationId = this.link.node2LocationId
            let p:(m: Branch)=>boolean = m=>m.node1LocationId>=0 && m.node2LocationId>=0 && 
                ( m.node1LocationId===node1LocationId && m.node2LocationId===node2LocationId) ||
                ( m.node1LocationId===node2LocationId && m.node2LocationId===node1LocationId)
            this._branches = this.dataService.branches.data.filter(p)
            this._deletedBranches = this.dataService.branches.deletedData.filter(p)

        }
    }

}
