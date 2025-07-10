import { Component, Inject, OnInit } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DatasetDialogComponent } from 'src/app/datasets/dataset-dialog/dataset-dialog.component';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { Branch, Ctrl, LoadflowCtrlType, Node, Zone } from 'src/app/data/app.data';
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
        //
        this.nodes1 = this.loadflowService.networkData.nodes.data
        this.nodes2 = this.nodes1
        this.zones = this.loadflowService.networkData.zones.data
        //
        this.controlTypes = this.getControlTypes([LoadflowCtrlType.DecInc,LoadflowCtrlType.InterTrip,LoadflowCtrlType.Transfer])
        //
        this.fType = this.addFormControl('type')
        this.fNodeId1 = this.addFormControl('nodeId1')
        this.fNodeId1.addValidators([Validators.required])
        this.fNodeId2 = this.addFormControl('nodeId2')
        this.fNodeId2.addValidators([Validators.required])
        this.fZoneId1 = this.addFormControl('zoneId1')
        this.fZoneId2 = this.addFormControl('zoneId2')
        this.fGpc1 = this.addFormControl('gpc1')
        this.fGpc2 = this.addFormControl('gpc2')
        this.fMinCtrl = this.addFormControl('minCtrl')
        this.fMaxCtrl = this.addFormControl('maxCtrl')
        this.fCost = this.addFormControl('cost')
        if ( dialogData?._data ) {
            let data:Ctrl = dialogData._data
            this.title = `Edit control [${data.displayName}]`
            this.fType.setValue(data.type)
            console.log('ctrl',data)
            if ( data.n1!=null) {
                this.fNodeId1.setValue(data.n1.id)
            }
            if (data.n2 != null) {
                this.fNodeId2.setValue(data.n2.id)
            }
            if (data.z1 != null) {
                this.fZoneId1.setValue(data.z1.id)
            }
            if (data.z2 != null) {
                this.fZoneId2.setValue(data.z2.id)
            }
            this.fGpc1.setValue(data.gpC1.toFixed(1))
            this.fGpc2.setValue(data.gpC2.toFixed(1))
            this.fMinCtrl.setValue(data.minCtrl.toFixed(2))
            this.fMaxCtrl.setValue(data.maxCtrl.toFixed(2))
            this.fCost.setValue(data.cost.toFixed(1))
        } else {
            this.title = `Add control`
            this.fType.setValue(LoadflowCtrlType.DecInc)
            this.fType.markAsDirty()
            this.fMinCtrl.setValue(0)
            this.fMaxCtrl.setValue(0)
            this.fCost.setValue(10)
            this.fCost.markAsDirty()
        }
        this.dialogData = dialogData
        // disable controls not user-editable
        if ( this.dialogData && !this.dialogData._isLocalDataset ) {

        }
    }

    title: string
    loadflowCtrlType = LoadflowCtrlType
    controlTypes: { id: number, name: string }[] = []
    nodes1:Node[] = []
    nodes2:Node[] = []
    zones: Zone[] = []
    fType: FormControl
    fZoneId1: FormControl
    fZoneId2: FormControl
    fGpc1: FormControl
    fGpc2: FormControl
    fNodeId1: FormControl
    fNodeId2: FormControl
    fMinCtrl: FormControl
    fMaxCtrl: FormControl
    fCost: FormControl

    typeChanged() {

    }

    displayNode(n: any) {
        if (n.code) {
            return n.code
        } else {
            return "??"
        }
    }

    displayZone(z: any) {
        if (z.code) {
            return z.code
        } else {
            return "??"
        }
    }

    node1Changed(e: any) {
    }

    node2Changed(e: any) {
    }

    zone1Changed(e: any) {
    }

    zone2Changed(e: any) {
    }

    CtrlType = LoadflowCtrlType

    get type():LoadflowCtrlType {
        return this.fType.value
    }

    private getControlTypes(types: LoadflowCtrlType[]):{id: number, name: string}[] {
        let bts:{id: number, name: string}[] = []
        for( let type of types) {
            bts.push({ id: type, name: LoadflowCtrlType[type]});
        }
        return bts;
    }

    getBranchStr(b: any):string {
        return b.lineName
    }

    save() {
        if ( this.datasetsService.currentDataset) {
            let changedControls = this.getUpdatedControls()
            let id = this.dialogData?._data ? this.dialogData._data.id : 0
            this.loadflowService.saveDialog(id, "Ctrl",changedControls, () => {
                this.dialogRef.close()
            }, (errors) => {
                this.fillErrors(errors)
            })

        }
    }


}
