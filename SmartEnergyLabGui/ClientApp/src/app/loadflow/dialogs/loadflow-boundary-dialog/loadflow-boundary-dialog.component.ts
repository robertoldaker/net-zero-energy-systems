import { Component, Inject, OnInit } from '@angular/core';
import { Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DatasetDialogComponent } from 'src/app/datasets/dataset-dialog/dataset-dialog.component';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { Boundary, Zone } from 'src/app/data/app.data';

@Component({
    selector: 'app-loadflow-boundary-dialog',
    templateUrl: './loadflow-boundary-dialog.component.html',
    styleUrls: ['../../../dialogs/dialog-base.css','./loadflow-boundary-dialog.component.css']
})
export class LoadflowBoundaryDialogComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>, 
        @Inject(MAT_DIALOG_DATA) dialogData:ICellEditorDataDict | undefined,
        private dataService: DataClientService, 
        private loadflowService: LoadflowDataService,
        private datasetsService: DatasetsService
    ) { 
        super()
        let fCode = this.addFormControl('code')
        fCode.addValidators( [Validators.required]) // Adds the start next to the control
        let fZoneIds = this.addFormControl('zoneIds')
        fZoneIds.addValidators( [Validators.required]) // Adds the start next to the control
        if ( dialogData?._data ) {
            let data:Boundary = dialogData._data
            this.title = `Edit boundary [${data.code}]`
            fCode.setValue(data.code)
            let zoneIds:number[] = []
            data.zones.forEach((z)=>{ zoneIds.push(z.id) })
            fZoneIds.setValue(zoneIds)            
        } else {
            this.title = `Add boundary`
            fCode.setValue("")
        }
        this.dialogData = dialogData
        // disable controls not user-editable
        if ( this.dialogData && !this.dialogData._isLocalDataset ) {
            fCode.disable()
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
            this.dataService.EditItem({id: id, datasetId: this.datasetsService.currentDataset.id, className: "Boundary", data: changedControls }, (resp)=>{
                this.loadflowService.afterEdit(resp)
                this.dialogRef.close();
            }, (errors)=>{
                this.fillErrors(errors)
            })
            
        }
    }

}
