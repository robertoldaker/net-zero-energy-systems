import { Component, Inject, OnInit } from '@angular/core';
import { Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DatasetDialogComponent } from 'src/app/datasets/dataset-dialog/dataset-dialog.component';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { Branch, Ctrl, LoadflowCtrlType } from 'src/app/data/app.data';
import { ISearchResults } from 'src/app/datasets/dialog-auto-complete/dialog-auto-complete.component';

@Component({
  selector: 'app-loadflow-ctrl-dialog',
  templateUrl: './loadflow-ctrl-dialog.component.html',
  styleUrls: ['../../../dialogs/dialog-base.css','./loadflow-ctrl-dialog.component.css']
})
export class LoadflowCtrlDialogComponent extends DialogBase {

    constructor(
        public dialogRef: MatDialogRef<DatasetDialogComponent>, 
        @Inject(MAT_DIALOG_DATA) dialogData:ICellEditorDataDict | undefined,
        private dataService: DataClientService, 
        private loadflowService: LoadflowDataService,
        private datasetsService: DatasetsService
    ) { 
        super()
        let fBranch = this.addFormControl('branchId')
        let fType = this.addFormControl('type')
        let fMinCtrl = this.addFormControl('minCtrl')
        let fMaxCtrl = this.addFormControl('maxCtrl')
        let fCost = this.addFormControl('cost')
        this.branches = this.loadflowService.getBranchesWithoutCtrls()
        if ( dialogData?._data ) {
            let data:Ctrl = dialogData._data
            this.title = `Edit control [${data.lineName}]`
            let branch = this.loadflowService.networkData.branches.data.find(m=>m.lineName == data.lineName)
            if ( branch) {
                fBranch.setValue(branch.id)
                this.branches.push(branch) // Need to add it to the list since it won't be in the list otherwise
                this.selectedBranch = branch
            }
            fType.setValue(data.type)
            fMinCtrl.setValue(data.minCtrl)
            fMaxCtrl.setValue(data.maxCtrl)
            fCost.setValue(data.cost)

        } else {
            this.title = `Add control`
            fType.setValue(LoadflowCtrlType.QB)
            fMinCtrl.setValue(0)
            fMaxCtrl.setValue(0)
            fCost.setValue(10)
        }
        this.dialogData = dialogData
        // disable controls not user-editable
        if ( this.dialogData && !this.dialogData._isLocalDataset ) {
            fBranch.disable()
            fType.disable()
        }
    }

    title: string
    loadflowCtrlType = LoadflowCtrlType
    selectedBranch: Branch | undefined
    branches: Branch[]

    //?? Needed if using an autoComplete
    /*searchBranches(e: ISearchResults) {
        let lcStr = e.text.toLowerCase()
        let ctrlBranches = this.loadflowService.networkData.branches.data.filter( m=>m.node1Code.substring(0,5) == m.node2Code.substring(0,5))
        let brs = ctrlBranches.filter(
            m=>m.node1Code.toLowerCase().includes(lcStr) || 
            m.node1Code.toLowerCase().includes(lcStr) || 
            (m.code && m.code.toLowerCase().includes(lcStr)))
        e.results = brs
        return
    }
    */

    branchSelected(e: Branch) {
        this.selectedBranch = e
        this.updateCtrls()
    }

    updateCtrls() {
        if ( this.selectedBranch) {
            let type = this.form.get('type')?.value  
            if ( type == LoadflowCtrlType.HVDC) {
                this.form.get('minCtrl')?.setValue(-this.selectedBranch.cap)
                this.form.get('maxCtrl')?.setValue(this.selectedBranch.cap)    
            } else if ( type == LoadflowCtrlType.QB) {
                let v = this.selectedBranch.node1Code.charAt(4);
                let ctrl:number = v == '4' ? 0.2 : 0.15
                this.form.get('minCtrl')?.setValue(-ctrl)
                this.form.get('maxCtrl')?.setValue(ctrl)    
            } 
            this.form.get('minCtrl')?.markAsDirty()
            this.form.get('maxCtrl')?.markAsDirty()  
        }
    }

    getBranchStr(b: any):string {
        return b.lineName
    }

    typeSelected(e: any) {
        this.updateCtrls()
    }


    save() {
        if ( this.datasetsService.currentDataset) {
            let changedControls = this.getUpdatedControls()

            let id = this.dialogData?._data ? this.dialogData._data.id : 0
            this.dataService.EditItem({id: id, datasetId: this.datasetsService.currentDataset.id, className: "Ctrl", data: changedControls }, (resp)=>{
                this.datasetsService.refreshData()
                this.dialogRef.close();
            }, (errors)=>{
                this.fillErrors(errors)
            })
            
        }
    }


}
