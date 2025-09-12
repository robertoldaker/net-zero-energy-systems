import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DatasetDialogComponent } from 'src/app/datasets/dataset-dialog/dataset-dialog.component';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { Zone } from 'src/app/data/app.data';
import { BoundCalcDataService } from '../../boundcalc-data-service.service';

@Component({
    selector: 'app-boundcalc-zone-dialog',
    templateUrl: './boundcalc-zone-dialog.component.html',
    styleUrls: ['../../../dialogs/dialog-base.css','./boundcalc-zone-dialog.component.css']
})
export class BoundCalcZoneDialogComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>,
        @Inject(MAT_DIALOG_DATA) dialogData: ICellEditorDataDict | undefined,
        private dataService: DataClientService,
        private boundcalcService: BoundCalcDataService,
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
            this.boundcalcService.saveDialog(id, "Zone",changedControls, (obj) => {
                this.dialogRef.close(obj)
            }, (errors) => {
                this.fillErrors(errors)
            })

        }
    }

}
