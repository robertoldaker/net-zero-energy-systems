import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Dataset, GridSubstationLocation, Node, Zone } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { DatasetDialogComponent } from 'src/app/datasets/dataset-dialog/dataset-dialog.component';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { BoundCalcDataService } from '../../boundcalc-data-service.service';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { ICellEditorDataDict} from 'src/app/datasets/cell-editor/cell-editor.component';
import { Validators } from '@angular/forms';

@Component({
    selector: 'app-boundcalc-location-dialog',
    templateUrl: './boundcalc-location-dialog.component.html',
    styleUrls: ['../../../dialogs/dialog-base.css','./boundcalc-location-dialog.component.css']
})
export class BoundCalcLocationDialogComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>,
        @Inject(MAT_DIALOG_DATA) dialogData:ICellEditorDataDict | undefined,
        private dataService: DataClientService,
        private boundcalcService: BoundCalcDataService,
        private datasetsService: DatasetsService
    ) {
        super()
        let fCode = this.addFormControl('code')
        fCode.addValidators( [Validators.required])
        let fName = this.addFormControl('name')
        fName.addValidators( [Validators.required])
        let fLatitude = this.addFormControl('latitude')
        let fLongitude = this.addFormControl('longitude')
        if ( dialogData?._data?.id ) {
            let data:GridSubstationLocation = dialogData._data
            this.title = `Edit location [${data.reference}]`
            fCode.setValue(data.reference)
            fName.setValue(data.name)
            fLatitude.setValue(data.latitude.toFixed(5))
            fLongitude.setValue(data.longitude.toFixed(5))
        } else {
            this.title = `Add location`
            fCode.setValue('')
            fCode.markAsDirty()
            fName.setValue('')
            fName.markAsDirty()
            let lat = dialogData?._data?.lat ? dialogData._data.lat : 0;
            let lng = dialogData?._data?.lng ? dialogData._data.lng : 0;
            fLatitude.setValue(lat.toFixed(5))
            fLatitude.markAsDirty()
            fLongitude.setValue(lng.toFixed(5))
            fLongitude.markAsDirty()
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
            this.boundcalcService.saveDialog(id, "GridSubstationLocation",changedControls, () => {
                this.dialogRef.close()
            }, (errors) => {
                this.fillErrors(errors)
            })

        }
    }

}

