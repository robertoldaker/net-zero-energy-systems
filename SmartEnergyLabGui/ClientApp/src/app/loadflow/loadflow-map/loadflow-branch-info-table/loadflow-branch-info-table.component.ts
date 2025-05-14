import { Component, Input, OnInit } from '@angular/core';
import { Branch } from 'src/app/data/app.data';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { LoadflowDataService } from '../../loadflow-data-service.service';

@Component({
    selector: 'app-loadflow-branch-info-table',
    templateUrl: './loadflow-branch-info-table.component.html',
    styleUrls: ['./loadflow-branch-info-table.component.css']
})
export class LoadflowBranchInfoTableComponent {

    constructor(
        private dataService: LoadflowDataService,
        private dialogService: DialogService
    ) { }

    @Input()
    branches:Branch[] = []

    edit(b: Branch) {
        let itemData = this.dataService.getBranchEditorData(b.id)
        this.dialogService.showLoadflowBranchDialog(itemData)
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

    get totalFlowStr():string {
        let tf = this.getTotalFlow()
        if( tf!=null) {
            return tf.toFixed(0)
        } else {
            return ''
        }
    }

    private getTotalFlow():number | null {
        let tf = null
        for( let b of this.branches) {
            if ( b.powerFlow!=null ) {
                if ( tf==null) {
                    tf = 0
                }
                tf += b.powerFlow
            }
        }
        return tf
    }

    get totalFreeStr():string {
        let tf = this.getTotalFree()
        if ( tf!=null ) {
            return tf.toFixed(0)
        } else {
            return ''
        }
    }

    private getTotalFree():number | null {
        let tf = null
        for( let b of this.branches) {
            if ( b.freePower!=null ) {
                if ( tf==null) {
                    tf = 0
                }
                tf += b.freePower
            }
        }
        return tf
    }


}
