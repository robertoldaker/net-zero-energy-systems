import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Dataset, Node, Zone } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { DatasetDialogComponent } from 'src/app/datasets/dataset-dialog/dataset-dialog.component';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { ICellEditorDataDict} from 'src/app/datasets/cell-editor/cell-editor.component';
import { Validators } from '@angular/forms';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
    selector: 'app-loadflow-node-dialog',
    templateUrl: './loadflow-node-dialog.component.html',
    styleUrls: ['../../../dialogs/dialog-base.css','./loadflow-node-dialog.component.css']
})
export class LoadflowNodeDialogComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>, 
        @Inject(MAT_DIALOG_DATA) dialogData:ICellEditorDataDict | undefined,
        private dataService: DataClientService, 
        private loadflowService: LoadflowDataService,
        private datasetsService: DatasetsService,
        private dialogService: DialogService
    ) { 
        super()
        let fCode = this.addFormControl('code')
        let fDemand = this.addFormControl('demand')
        let fGeneration_A = this.addFormControl('generation_A')
        let fGeneration_B = this.addFormControl('generation_B')
        let fZoneId = this.addFormControl('zoneId')
        let fExt = this.addFormControl('ext')
        fCode.addValidators( [Validators.required])
        if ( dialogData?._data?.id ) {
            let data:Node = dialogData._data
            this.title = `Edit node [${data.code}]`
            fCode.setValue(data.code)
            fDemand.setValue(data.demand.toFixed(0))
            fGeneration_A.setValue(data.generation_A.toFixed(0))
            fGeneration_B.setValue(data.generation_B.toFixed(0))
            fZoneId.setValue(data.zone?.id)
            fExt.setValue(data.ext)
        } else {
            this.title = `Add node`
            let code = dialogData?._data.code ? dialogData._data.code : ''
            fCode.setValue(code)
            fCode.markAsDirty()
            fDemand.setValue(0)
            fGeneration_A.setValue(0)
            fZoneId.setValue("")
            fExt.setValue(false)
        }
        this.dialogData = dialogData
        // disable controls not user-editable
        if ( this.dialogData && !this.dialogData._isLocalDataset ) {
            fCode.disable()
            fZoneId.disable()
            fExt.disable()
        }
        this.zones = loadflowService.networkData.zones.data;
    }

    ngOnInit(): void {
    }    

    title: string
    zones: Zone[] = []
    zoneId: string | undefined

    displayZone(z: any) {
        if ( z.code ) {
            return z.code
        } else {
            return "??"
        }
    }

    save() {
        if ( this.datasetsService.currentDataset) {
            let changedControls = this.getUpdatedControls()

            let id = this.dialogData?._data ? this.dialogData._data.id : 0
            this.dataService.EditItem({id: id, datasetId: this.datasetsService.currentDataset.id, className: "Node", data: changedControls }, (resp)=>{
                this.loadflowService.afterEdit(resp)
                this.dialogRef.close();
            }, (errors)=>{
                this.fillErrors(errors)
            })
            
        }
    }

    addZone() {
        this.dialogService.showLoadflowZoneDialog()
    }

}
