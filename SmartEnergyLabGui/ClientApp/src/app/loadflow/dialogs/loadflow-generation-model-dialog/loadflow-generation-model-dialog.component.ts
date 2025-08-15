import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Dataset, GenerationModel, Zone } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { DatasetDialogComponent } from 'src/app/datasets/dataset-dialog/dataset-dialog.component';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { ICellEditorDataDict} from 'src/app/datasets/cell-editor/cell-editor.component';
import { Validators } from '@angular/forms';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
    selector: 'app-loadflow-generation-model-dialog',
    templateUrl: './loadflow-generation-model-dialog.component.html',
    styleUrls: ['../../../dialogs/dialog-base.css','./loadflow-generation-model-dialog.component.css']
})
export class LoadflowGenerationModelDialogComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>,
        @Inject(MAT_DIALOG_DATA) dialogData:GenerationModel | undefined,
        private dataService: DataClientService,
        private loadflowService: LoadflowDataService,
        private datasetsService: DatasetsService,
        private dialogService: DialogService
    ) {
        super()
        let fName = this.addFormControl('name')
        fName.addValidators( [Validators.required])
        if ( dialogData?.id ) {
            this.tm = dialogData
            this.title = `Edit generation model [${this.tm.name}]`
            fName.setValue(this.tm.name)
        } else {
            this.title = `Add generation model`
            let name = dialogData?.name ? dialogData.name : ''
            fName.setValue(name)
        }
    }

    ngOnInit(): void {
    }

    title: string
    tm: GenerationModel | undefined

    save() {
        if ( this.datasetsService.currentDataset) {
            let changedControls = this.getUpdatedControls()

            let id:number = this.tm ? this.tm.id : 0
            this.loadflowService.saveDialog(id, "GenerationModel",changedControls, () => {
                this.dialogRef.close()
            }, (errors) => {
                this.fillErrors(errors)
            })

        }
    }
}

