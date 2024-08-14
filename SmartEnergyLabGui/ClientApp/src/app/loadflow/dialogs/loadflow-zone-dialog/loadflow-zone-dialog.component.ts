import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DatasetDialogComponent } from 'src/app/datasets/dataset-dialog/dataset-dialog.component';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { Zone } from 'src/app/data/app.data';
import { LoadflowDataService } from '../../loadflow-data-service.service';

@Component({
    selector: 'app-loadflow-zone-dialog',
    templateUrl: './loadflow-zone-dialog.component.html',
    styleUrls: ['../../../dialogs/dialog-base.css','./loadflow-zone-dialog.component.css']
})
export class LoadflowZoneDialogComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>,
        @Inject(MAT_DIALOG_DATA) dialogData: ICellEditorDataDict | undefined,
        private dataService: DataClientService,
        private loadflowService: LoadflowDataService,
        private datasetsService: DatasetsService) { 
        super()
        let fCode = this.addFormControl('code')
        if ( dialogData?._data ) {
            let data:Zone = dialogData._data
            this.title = `Edit zone [${data.code}]`
            fCode.setValue(data.code)
        } else {
            this.title = `Add zone`
            fCode.setValue("")
        }
        this.dialogData = dialogData
        // disable controls not user-editable
        if ( this.dialogData && !this.dialogData._isLocalDataset ) {
            fCode.disable()
        }
    }

    ngOnInit(): void {
    }

    title: string

    save() {
        if ( this.datasetsService.currentDataset) {
            let changedControls = this.getUpdatedControls()
            let id = this.dialogData?._data ? this.dialogData._data.id : 0
            this.dataService.EditItem({id: id, datasetId: this.datasetsService.currentDataset.id, className: "Zone", data: changedControls }, (resp)=>{
                this.loadflowService.afterEdit(resp)
                this.dialogRef.close();
            }, (errors)=>{
                this.fillErrors(errors)
            })
            
        }
    }

}
