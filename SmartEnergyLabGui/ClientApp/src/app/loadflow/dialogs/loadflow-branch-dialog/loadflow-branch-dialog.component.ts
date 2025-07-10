import { Component, Inject, OnInit } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
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
        this.fType = this.addFormControl('type')
        this.fCode = this.addFormControl('code')
        this.fNodeId1 = this.addFormControl('nodeId1')
        this.fNodeId1.addValidators( [Validators.required])
        this.fNodeId2 = this.addFormControl('nodeId2')
        this.fNodeId2.addValidators( [Validators.required])
        this.fX = this.addFormControl('x')
        this.fCap = this.addFormControl('cap')
        this.fOHL = this.addFormControl('ohl')
        this.fCableLength = this.addFormControl('cableLength')
        // ctrl params
        this.fMinCtrl = this.addFormControl('minCtrl')
        this.fMaxCtrl = this.addFormControl('maxCtrl')
        this.fCost = this.addFormControl('cost')
        this.nodes1 = this.loadflowService.networkData.nodes.data
        this.nodes2 = this.nodes1
        if ( editorData?.branch?._data?.id ) {
            let data:Branch = editorData.branch._data
            this.title = `Edit branch [${data.displayName}]`
            this.fCode.setValue(data.code)
            this.fNodeId1.setValue(data.node1Id)
            this.fNodeId2.setValue(data.node2Id)
            this.fX.setValue(data.x.toFixed(3))
            this.fCap.setValue(data.cap.toFixed(3))
            this.fOHL.setValue(data.ohl.toFixed(0))
            this.fCableLength.setValue(data.cableLength.toFixed(0))
            if ( editorData?.ctrl?._data ) {
                let ctrl:Ctrl = editorData.ctrl._data
                this.fMinCtrl.setValue(ctrl.minCtrl)
                this.fMaxCtrl.setValue(ctrl.maxCtrl)
                this.fCost.setValue(ctrl.cost)
                this.isCtrl = true
            }
            this.needsLength = this.isLengthType(data.type)
            this.updateType()
            this.fType.setValue(data.type)
        } else {
            this.title = `Add branch`
            let node1 = editorData?.branch?._data?.node1 ? editorData.branch._data.node1 : ''
            if ( node1 ) {
                this.nodes1 = this.filterNodes(node1,this.nodes1)
                if ( this.nodes1.length == 1) {
                    this.fNodeId1.setValue(this.nodes1[0].id)
                    this.fNodeId1.markAsDirty()
                }
            }
            let node2 = editorData?.branch?._data?.node2 ? editorData.branch._data.node2 : ''
            if ( node2 ) {
                this.nodes2 = this.filterNodes(node2,this.nodes2)
                if ( this.nodes2.length == 1) {
                    this.fNodeId2.setValue(this.nodes2[0].id)
                    this.fNodeId2.markAsDirty()
                }
            }
            this.updateType()
            this.fX.setValue(0.1)
            this.fX.markAsDirty()
            this.fCap.setValue(100)
            this.fCap.markAsDirty()
            this.fCost.setValue(10)
            this.fCost.markAsDirty()
        }
        // disable controls not user-editable
        if ( editorData?.branch && !editorData.branch._isLocalDataset ) {
            this.fCode.disable()
            this.fNodeId1.disable()
            this.fNodeId2.disable()
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

    fType: FormControl
    fCode: FormControl
    fNodeId1: FormControl
    fNodeId2: FormControl
    fX: FormControl
    fCap: FormControl
    fOHL: FormControl
    fCableLength: FormControl
    fMinCtrl: FormControl
    fMaxCtrl: FormControl
    fCost: FormControl

    isCtrl: boolean = false
    needsLength: boolean = false
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

    typeChanged() {
        let type = this.fType.value
        this.isCtrl = this.isCtrlType(type)
        if ( this.isCtrl) {
            this.updateMinMaxCtrl()
        }
        this.needsLength = this.isLengthType(type)
        if ( this.needsLength) {
            this.updateDist()
        }
    }

    private isCtrlType(type: BranchType) {
        return type === BranchType.QB || type === BranchType.HVDC || type == BranchType.SeriesCapacitor
    }

    private isLengthType(type: BranchType) {
        return type === BranchType.OHL || type === BranchType.Composite || type == BranchType.Cable
    }

    updateMinMaxCtrl() {
        let type = this.fType.value
        if ( type == BranchType.HVDC) {
            let cap = this.fCap.value
            let capacity = parseFloat(cap)
            if ( capacity ) {
                this.fMinCtrl.setValue(-capacity)
                this.fMinCtrl.markAsDirty()
                this.fMaxCtrl.setValue(capacity)
                this.fMaxCtrl.markAsDirty()
            }
            this.fCost.setValue(20.0)
            this.fCost.markAsDirty()
        } else if ( type == BranchType.QB || type == BranchType.SeriesCapacitor) {
            let nodeId1 = this.fNodeId1.value
            let node1 = this.nodes1.find(m=>m.id == nodeId1);
            if ( node1  ) {
                let ctrl = node1.voltage < 400 ? 0.15 : 0.2
                this.fMinCtrl.setValue(-ctrl);
                this.fMinCtrl.markAsDirty()
                this.fMaxCtrl.setValue(ctrl);
                this.fMaxCtrl.markAsDirty()
            }
            this.fCost.setValue(10.0)
            this.fCost.markAsDirty()
        }
    }

    node1Changed(e: any) {
        this.updateType()
        this.updateDist()
    }

    node2Changed(e: any) {
        this.updateType()
        this.updateDist()
    }

    private updateDist() {
        let nodeId1 = this.fNodeId1.value
        let nodeId2 = this.fNodeId2.value
        if ( nodeId1 && nodeId2 ) {
            this.dataService.DistBetweenNodes(nodeId1, nodeId2, (dist) => {
                let type = this.fType.value;
                if (dist > 0) {
                    if (type === BranchType.OHL) {
                        this.fOHL.setValue(dist.toFixed(0))
                        this.fCableLength.setValue(0)
                    } else if (type === BranchType.Cable) {
                        this.fCableLength.setValue(dist.toFixed(0))
                        this.fOHL.setValue(0)
                    } else if (type === BranchType.Composite) {
                        this.fOHL.setValue((dist / 2).toFixed(0))
                        this.fCableLength.setValue((dist / 2).toFixed(0))
                    }
                } else {
                    this.fOHL.setValue(0)
                    this.fCableLength.setValue(0)
                }
                this.fOHL.markAsDirty()
                this.fCableLength.markAsDirty()
            })
        }
    }

    private updateType() {
        let nodeId1 = this.fNodeId1.value
        let node1 = this.nodes1.find(m=>m.id == nodeId1);

        let nodeId2 = this.fNodeId2.value
        let node2 = this.nodes2.find(m=>m.id == nodeId2);

        if ( node1 && node2 ) {
            let node1Loc = this.getLocCode(node1);
            let node2Loc = this.getLocCode(node2);
            if ( node1Loc == node2Loc) {
                if ( node1.voltage == node2.voltage) {
                    this.branchTypes = this.getBranchTypes([BranchType.QB,BranchType.SeriesCapacitor])
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
            // check current type is in the list
            let branchType = this.fType.value
            if ( branchType ) {
                if ( this.branchTypes.length>0) {
                    let nt = this.branchTypes.find(m => m.id === branchType)
                    if (!nt) {
                        this.fType.setValue(this.branchTypes[0].id)
                        this.fType.markAsDirty()
                        this.typeChanged()
                    }
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
