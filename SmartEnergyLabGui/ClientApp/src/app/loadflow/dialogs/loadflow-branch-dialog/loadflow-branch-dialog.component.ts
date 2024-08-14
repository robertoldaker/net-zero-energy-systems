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
import { ISearchResults } from 'src/app/datasets/dialog-auto-complete/dialog-auto-complete.component';

@Component({
  selector: 'app-loadflow-branch-dialog',
  templateUrl: './loadflow-branch-dialog.component.html',
  styleUrls: ['../../../dialogs/dialog-base.css','./loadflow-branch-dialog.component.css']
})

export class LoadflowBranchDialogComponent extends DialogBase {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>, 
        @Inject(MAT_DIALOG_DATA) dialogData:ICellEditorDataDict | undefined,
        private dataService: DataClientService, 
        private loadflowService: LoadflowDataService,
        private datasetsService: DatasetsService
    ) { 
        super()
        let fCode = this.addFormControl('code')
        let fNodeId1 = this.addFormControl('nodeId1')
        fNodeId1.addValidators( [Validators.required])
        let fNodeId2 = this.addFormControl('nodeId2')
        fNodeId2.addValidators( [Validators.required])
        let fX = this.addFormControl('x')
        let fCap = this.addFormControl('cap')
        this.nodes1 = this.loadflowService.networkData.nodes.data
        this.nodes2 = this.nodes1
        if ( dialogData?._data?.id ) {
            let data:Branch = dialogData._data
            this.title = `Edit branch [${data.displayName}]`
            fCode.setValue(data.code)
            fNodeId1.setValue(data.node1Id)
            fNodeId2.setValue(data.node2Id)
            fX.setValue(data.x.toFixed(3))
            fCap.setValue(data.cap.toFixed(3))
        } else {
            this.title = `Add branch`
            let node1 = dialogData?._data?.node1 ? dialogData._data.node1 : ''
            if ( node1 ) {
                this.nodes1 = this.filterNodes(node1,this.nodes1)
                if ( this.nodes1.length == 1) {
                    fNodeId1.setValue(this.nodes1[0].id)
                    fNodeId1.markAsDirty()
                }
            }
            let node2 = dialogData?._data?.node2 ? dialogData._data.node2 : ''
            if ( node2 ) {
                this.nodes2 = this.filterNodes(node2,this.nodes2)
                if ( this.nodes2.length == 1) {
                    fNodeId2.setValue(this.nodes2[0].id)
                    fNodeId2.markAsDirty()
                }
            }
            fX.setValue(0)
            fCap.setValue(0)
        }

        this.dialogData = dialogData
        // disable controls not user-editable
        if ( this.dialogData && !this.dialogData._isLocalDataset ) {
            fCode.disable()
            fNodeId1.disable()
            fNodeId2.disable()
        }
    }

    filterNodes(nodeRef: string, nodes: Node[]):Node[] {
        if ( nodeRef.length > 4) {
            let snode1 = nodeRef.substring(0,4)
            let enode1 = nodeRef.substring(4,5)
            return nodes.filter( m=>m.code.startsWith(snode1) && m.code.endsWith(enode1))
        } else {
            return nodes.filter( m=>m.code.startsWith(nodeRef))
        }
    }

    title: string

    displayNode(n: any) {
        if ( n.code ) {
            return n.code
        } else {
            return "??"
        }
    }

    nodes1:Node[] = []
    nodes2:Node[] = []

    save() {
        if ( this.datasetsService.currentDataset) {
            let changedControls = this.getUpdatedControls()

            let id = this.dialogData?._data ? this.dialogData._data.id : 0
            this.dataService.EditItem({id: id, datasetId: this.datasetsService.currentDataset.id, className: "Branch", data: changedControls }, (resp)=>{
                this.loadflowService.afterEdit(resp)
                this.dialogRef.close();
            }, (errors)=>{
                this.fillErrors(errors)
            })
            
        }
    }

}
