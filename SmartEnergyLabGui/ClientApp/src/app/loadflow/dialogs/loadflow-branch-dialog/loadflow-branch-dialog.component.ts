import { Component, Inject, OnInit } from '@angular/core';
import { Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DatasetDialogComponent } from 'src/app/datasets/dataset-dialog/dataset-dialog.component';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { Branch, Node } from 'src/app/data/app.data';

@Component({
  selector: 'app-loadflow-branch-dialog',
  templateUrl: './loadflow-branch-dialog.component.html',
  styleUrls: ['../../../dialogs/dialog-base.css','./loadflow-branch-dialog.component.css']
})

export class LoadflowBranchDialogComponent extends DialogBase {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>, 
        @Inject(MAT_DIALOG_DATA) dialogData:ICellEditorDataDict | undefined,
        private dataService: DataClientService, 
        loadflowService: LoadflowDataService,
        private datasetsService: DatasetsService
    ) { 
        super()
        let fCode = this.addFormControl('code')
        let fNode1 = this.addFormControl('nodeId1')
        fNode1.addValidators( [Validators.required])
        let fNode2 = this.addFormControl('nodeId2')
        fNode2.addValidators( [Validators.required])
        let fX = this.addFormControl('x')
        let fCap = this.addFormControl('cap')
        if ( dialogData?._data ) {
            let data:Branch = dialogData._data
            this.title = `Edit branch [${data.lineName}]`
            fCode.setValue(data.code)
            fNode1.setValue(data.node1Id)
            fNode2.setValue(data.node2Id)
            fX.setValue(data.x)
            fCap.setValue(data.cap)
        } else {
            this.title = `Add branch`
            fX.setValue(0)
            fCap.setValue(0)
        }
        this.dialogData = dialogData
        // disable controls not user-editable
        if ( this.dialogData && !this.dialogData._isLocalDataset ) {
            fCode.disable()
            fNode1.disable()
            fNode2.disable()
        }
        this.nodes = loadflowService.networkData.nodes.data;
    }

    title: string
    nodes: Node[] = []

    displayNode(n: any) {
        if ( n.code ) {
            return n.code
        } else {
            return "??"
        }
    }

    save() {
        if ( this.datasetsService.currentDataset) {
            let changedControls = this.getUpdatedControls()

            let id = this.dialogData?._data ? this.dialogData._data.id : 0
            this.dataService.EditItem({id: id, datasetId: this.datasetsService.currentDataset.id, className: "Branch", data: changedControls }, (resp)=>{
                this.datasetsService.refreshData()
                this.dialogRef.close();
            }, (errors)=>{
                this.fillErrors(errors)
            })
            
        }
    }

}
