import { Component, Inject, OnInit } from '@angular/core';
import { Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DatasetDialogComponent } from 'src/app/datasets/dataset-dialog/dataset-dialog.component';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { Generator, GeneratorType, Zone } from 'src/app/data/app.data';

@Component({
    selector: 'app-loadflow-generator-dialog',
    templateUrl: './loadflow-generator-dialog.component.html',
    styleUrls: ['../../../dialogs/dialog-base.css','./loadflow-generator-dialog.component.css']
})
export class LoadflowGeneratorDialogComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>,
        @Inject(MAT_DIALOG_DATA) dialogData:ICellEditorDataDict | undefined,
        private dataService: DataClientService,
        private loadflowService: LoadflowDataService,
        private datasetsService: DatasetsService
    ) {
        super()
        let fName = this.addFormControl('name')
        fName.addValidators( [Validators.required])
        let fCapacity = this.addFormControl('capacity')
        fCapacity.addValidators( [Validators.required])
        let fType = this.addFormControl('type')
        fType.addValidators( [Validators.required])
        if ( dialogData?._data ) {
            let data:Generator = dialogData._data
            this.title = `Edit generator [${data.name}]`
            fName.setValue(data.name)
            fCapacity.setValue(data.capacity.toFixed(0))
            fType.setValue(data.type)
        } else {
            this.title = `Add generator`
            fName.markAsDirty()
            fCapacity.setValue(1000)
            fCapacity.markAsDirty()
            fType.markAsDirty()
        }
        this.dialogData = dialogData
        // disable controls not user-editable
        if ( this.dialogData && !this.dialogData._isLocalDataset ) {
            fName.disable()
            fType.disable()
        }
    }

    ngOnInit(): void {
    }

    title: string
    generatorType =  GeneratorType

    save() {
        if ( this.datasetsService.currentDataset) {
            let changedControls = this.getUpdatedControls()
            let id = this.dialogData?._data ? this.dialogData._data.id : 0
            this.loadflowService.saveDialog(id, "Generator",changedControls, () => {
                this.dialogRef.close()
            }, (errors) => {
                this.fillErrors(errors)
            })

        }
    }

}
