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
        let fNode1 = this.addFormControl('node1')
        let fNodeId1 = this.addFormControl('nodeId1')
        fNode1.addValidators( [Validators.required])
        let fNode2 = this.addFormControl('node2')
        let fNodeId2 = this.addFormControl('nodeId2')
        fNode2.addValidators( [Validators.required])
        let fX = this.addFormControl('x')
        let fCap = this.addFormControl('cap')
        if ( dialogData?._data ) {
            let data:Branch = dialogData._data
            this.title = `Edit branch [${data.displayName}]`
            fCode.setValue(data.code)
            fNode1.setValue(data.node1Code)
            fNode2.setValue(data.node2Code)
            fX.setValue(data.x.toFixed(3))
            fCap.setValue(data.cap.toFixed(3))
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
    }

    title: string

    displayNode(n: any) {
        if ( n.code ) {
            return n.code
        } else {
            return "??"
        }
    }

    selectedNode1(e: any) {
        this.form.get('nodeId1')?.setValue(e.id)
        this.form.get('nodeId1')?.markAsDirty()
    }

    selectedNode2(e: any) {
        this.form.get('nodeId2')?.setValue(e.id)
        this.form.get('nodeId2')?.markAsDirty()
    }

    searchNodes(e: ISearchResults) {
        let nodes = this.loadflowService.networkData.nodes.data;

        let results:Node[] = [];
        let searchText = e.text.toLocaleUpperCase()
        nodes.forEach(m=>{
            if ( m.code.toUpperCase().startsWith(searchText) && results.length<50) {
                results.push(m);
                return true;
            } else {
                return false;
            }
        });        
        results.sort((a, b) => a.code.localeCompare(b.code)); 
        e.results = results;
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
