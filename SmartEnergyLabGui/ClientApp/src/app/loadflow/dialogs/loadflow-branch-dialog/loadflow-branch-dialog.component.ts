import { Component, Inject, OnInit } from '@angular/core';
import { Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DatasetDialogComponent } from 'src/app/datasets/dataset-dialog/dataset-dialog.component';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { IBranchEditorData, LoadflowDataService } from '../../loadflow-data-service.service';
import { Branch, BranchType, Ctrl, Node } from 'src/app/data/app.data';
import { ISearchResults } from 'src/app/datasets/dialog-auto-complete/dialog-auto-complete.component';

@Component({
  selector: 'app-loadflow-branch-dialog',
  templateUrl: './loadflow-branch-dialog.component.html',
  styleUrls: ['../../../dialogs/dialog-base.css','./loadflow-branch-dialog.component.css']
})

export class LoadflowBranchDialogComponent extends DialogBase {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>,
        @Inject(MAT_DIALOG_DATA) editorData:IBranchEditorData | undefined,
        private dataService: DataClientService,
        private loadflowService: LoadflowDataService,
        private datasetsService: DatasetsService
    ) {
        super()
        let fType = this.addFormControl('type')
        let fCode = this.addFormControl('code')
        let fNodeId1 = this.addFormControl('nodeId1')
        fNodeId1.addValidators( [Validators.required])
        let fNodeId2 = this.addFormControl('nodeId2')
        fNodeId2.addValidators( [Validators.required])
        let fX = this.addFormControl('x')
        let fCap = this.addFormControl('cap')
        let fOHL = this.addFormControl('ohl')
        let fCableLength = this.addFormControl('cableLength')
        // ctrl params
        let fMinCtrl = this.addFormControl('minCtrl')
        let fMaxCtrl = this.addFormControl('maxCtrl')
        let fCost = this.addFormControl('cost')
        this.nodes1 = this.loadflowService.networkData.nodes.data
        this.nodes2 = this.nodes1
        if ( editorData?.branch?._data?.id ) {
            let data:Branch = editorData.branch._data
            this.title = `Edit branch [${data.displayName}]`
            fCode.setValue(data.code)
            fNodeId1.setValue(data.node1Id)
            fNodeId2.setValue(data.node2Id)
            fX.setValue(data.x.toFixed(3))
            fCap.setValue(data.cap.toFixed(3))
            fOHL.setValue(data.ohl.toFixed(0))
            fCableLength.setValue(data.cableLength.toFixed(0))
            if ( editorData?.ctrl?._data ) {
                let ctrl:Ctrl = editorData.ctrl._data
                fMinCtrl.setValue(ctrl.minCtrl)
                fMaxCtrl.setValue(ctrl.maxCtrl)
                fCost.setValue(ctrl.cost)
                this.isCtrl = true
            }
            this.updateType()
            fType.setValue(data.type)
            // don't allow editing as no mechanism for deleting a ctrl that might be left over
            fType.disable()
        } else {
            this.title = `Add branch`
            let node1 = editorData?.branch?._data?.node1 ? editorData.branch._data.node1 : ''
            if ( node1 ) {
                this.nodes1 = this.filterNodes(node1,this.nodes1)
                if ( this.nodes1.length == 1) {
                    fNodeId1.setValue(this.nodes1[0].id)
                    fNodeId1.markAsDirty()
                }
            }
            let node2 = editorData?.branch?._data?.node2 ? editorData.branch._data.node2 : ''
            if ( node2 ) {
                this.nodes2 = this.filterNodes(node2,this.nodes2)
                if ( this.nodes2.length == 1) {
                    fNodeId2.setValue(this.nodes2[0].id)
                    fNodeId2.markAsDirty()
                }
            }
            this.updateType()
            fX.setValue(0.1)
            fX.markAsDirty()
            fCap.setValue(100)
            fCap.markAsDirty()
            fCost.setValue(10)
            fCost.markAsDirty()
        }
        // disable controls not user-editable
        if ( editorData?.branch && !editorData.branch._isLocalDataset ) {
            fCode.disable()
            fNodeId1.disable()
            fNodeId2.disable()
        }
        this.editorData = editorData
        // need to merge branch and ctrl data so the base class can pickup user edits
        if ( editorData) {
            this.dialogData = Object.assign({},editorData.branch,editorData.ctrl);
        } else {
            this.dialogData = undefined
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

    isCtrl: boolean = false
    branchTypes:{id: number, name: string}[] = []
    editorData: IBranchEditorData | undefined

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

    typeChanged(e: any) {
        this.isCtrl = e.id == BranchType.QB || e.id == BranchType.HVDC
        if ( this.isCtrl) {
            this.updateMinMaxCtrl()
        }
        let fOHL = this.form.get('ohl')
        let fCableLength = this.form.get('cableLength')
        if ( e.id === BranchType.OHL || e.id ===BranchType.Composite || e.id === BranchType.Cable) {
            fOHL?.enable()
            fCableLength?.enable()
        } else {
            fOHL?.disable()
            fCableLength?.disable()
        }
        console.log('typeChanged',e)
    }

    updateMinMaxCtrl() {
        let type = this.form.get('type')?.value
        if ( type == BranchType.HVDC) {
            let cap = this.form.get('cap')?.value
            let capacity = parseFloat(cap)
            if ( capacity ) {
                let fMinCtrl = this.form.get('minCtrl')
                let fMaxCtrl = this.form.get('maxCtrl')
                if ( fMinCtrl && fMaxCtrl) {
                    fMinCtrl.setValue(-capacity)
                    fMinCtrl.markAsDirty()
                    fMaxCtrl.setValue(capacity)
                    fMaxCtrl.markAsDirty()
                }
            }
        } else if ( type == BranchType.QB) {
            let fMinCtrl = this.form.get('minCtrl')
            let fMaxCtrl = this.form.get('maxCtrl')
            if ( fMinCtrl && fMaxCtrl) {
                let nodeId1 = this.form.get('nodeId1')?.value
                let node1 = this.nodes1.find(m=>m.id == nodeId1);
                if ( node1  ) {
                    let ctrl = node1.voltage < 400 ? 0.15 : 0.2
                    fMinCtrl.setValue(-ctrl);
                    fMinCtrl.markAsDirty()
                    fMaxCtrl.setValue(ctrl);
                    fMaxCtrl.markAsDirty()
                }
            }
        }
    }

    node1Changed(e: any) {
        this.updateType()
    }

    node2Changed(e: any) {
        this.updateType()
    }

    private updateType() {
        let nodeId1 = this.form.get('nodeId1')?.value
        let node1 = this.nodes1.find(m=>m.id == nodeId1);

        let nodeId2 = this.form.get('nodeId2')?.value
        let node2 = this.nodes2.find(m=>m.id == nodeId2);

        if ( node1 && node2 ) {
            let node1Loc = this.getLocCode(node1);
            let node2Loc = this.getLocCode(node2);
            if ( node1Loc == node2Loc) {
                if ( node1.voltage == node2.voltage) {
                    this.branchTypes = this.getBranchTypes([BranchType.QB,BranchType.SSSC,BranchType.SeriesCapacitor,BranchType.SeriesReactor,BranchType.Other])
                } else {
                    this.branchTypes = this.getBranchTypes([BranchType.Transformer,BranchType.Other])
                }
            } else {
                if ( node1.voltage == node2.voltage ) {
                    this.branchTypes = this.getBranchTypes([BranchType.OHL,BranchType.Cable,BranchType.Composite,BranchType.HVDC,BranchType.Other])
                } else {
                    this.branchTypes = []
                }
            }
        }
    }

    private getLocCode(node: Node):string {
        let locCode = node.code.substring(0,4)
        if ( node.code.substring(5,6) === 'X' ) {
            locCode = locCode + 'X'
        }
        return locCode;
    }

    private getBranchTypes(types: BranchType[]):{id: number, name: string}[] {
        let bts:{id: number, name: string}[] = []
        for( let type of types) {
            bts.push({ id: type, name: BranchType[type]});
        }
        return bts;
    }

    onCapChange(e: any) {
        if ( this.isCtrl) {
            this.updateMinMaxCtrl()
        }
    }



    save() {
        if ( this.datasetsService.currentDataset) {
            let changedControls = this.getUpdatedControls()
            let id = this.editorData?.branch?._data ? this.editorData.branch._data.id : 0
            this.loadflowService.saveDialog(id, "Branch",changedControls, () => {
                this.dialogRef.close()
            }, (errors) => {
                this.fillErrors(errors)
            })

        }
    }

}
