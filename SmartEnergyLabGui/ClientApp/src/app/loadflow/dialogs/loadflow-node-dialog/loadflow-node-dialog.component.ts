import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Generator, Node, Zone } from 'src/app/data/app.data';
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
        fCode.addValidators( [Validators.required])
        let fDemand = this.addFormControl('demand')
        let fZoneId = this.addFormControl('zoneId')
        let fExt = this.addFormControl('ext')
        let fGeneratorIds = this.addFormControl('generatorIds')
        if ( dialogData?._data?.id ) {
            let data:Node = dialogData._data
            this.title = `Edit node [${data.code}]`
            fCode.setValue(data.code)
            fDemand.setValue(data.demand.toFixed(0))
            fZoneId.setValue(data.zone?.id)
            fExt.setValue(data.ext)
            let generatorIds:number[] = this.getGeneratorIds(data)
            fGeneratorIds.setValue(generatorIds)
        } else {
            this.title = `Add node`
            let code = dialogData?._data.code ? dialogData._data.code : ''
            fCode.setValue(code)
            fCode.markAsDirty()
            fDemand.setValue(0)
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
        this.zones = loadflowService.networkData.zones.data
        this.generators = loadflowService.networkData.generators.data
    }

    ngOnInit(): void {
    }

    getGeneratorIds(node: Node) {
        let genIds = node.generators.map(m=>m.id)
        return genIds
    }

    title: string
    zones: Zone[] = []
    zoneId: string | undefined
    generators: Generator[] = []

    displayZone(z: any) {
        if ( z.code ) {
            return z.code
        } else {
            return "??"
        }
    }

    displayGen(g: any) {
        if ( g.name ) {
            return g.name
        } else {
            return "??"
        }
    }

    save() {
        if ( this.datasetsService.currentDataset) {
            let changedControls = this.getUpdatedControls()
            changedControls['_transportModelId'] = this.loadflowService.transportModel?.id
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
