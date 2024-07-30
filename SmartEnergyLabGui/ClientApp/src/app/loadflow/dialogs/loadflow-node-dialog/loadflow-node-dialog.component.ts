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

@Component({
    selector: 'app-loadflow-node-dialog',
    templateUrl: './loadflow-node-dialog.component.html',
    styleUrls: ['../../../dialogs/dialog-base.css','./loadflow-node-dialog.component.css']
})
export class LoadflowNodeDialogComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>, 
        @Inject(MAT_DIALOG_DATA) dialogData:ICellEditorDataDict | undefined,
        private dataService: DataClientService, 
        loadflowService: LoadflowDataService,
        private datasetsService: DatasetsService
    ) { 
        super()
        let fCode = this.addFormControl('code')
        let fDemand = this.addFormControl('demand')
        let fGeneration = this.addFormControl('generation')
        let fZoneId = this.addFormControl('zoneId')
        let fExt = this.addFormControl('ext')
        fCode.addValidators( [Validators.required])
        if ( dialogData?._data ) {
            let data:Node = dialogData._data
            this.title = `Edit node [${data.code}]`
            fCode.setValue(data.code)
            fDemand.setValue(data.demand)
            fGeneration.setValue(data.generation)
            fZoneId.setValue(data.zone?.id)
            fExt.setValue(data.ext)
        } else {
            this.title = `Add node`
            fCode.setValue("")
            fDemand.setValue(0)
            fGeneration.setValue(0)
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
                this.datasetsService.refreshData()
                this.dialogRef.close();
            }, (errors)=>{
                this.fillErrors(errors)
            })
            
        }
    }

}
