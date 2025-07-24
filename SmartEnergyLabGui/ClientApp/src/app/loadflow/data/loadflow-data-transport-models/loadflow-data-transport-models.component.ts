import { Component, OnInit } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { DatasetData, GeneratorType, TransportModel, TransportModelEntry } from 'src/app/data/app.data';
import { MatTableDataSource } from '@angular/material/table';
import { DataTableBaseComponent } from 'src/app/datasets/data-table-base/data-table-base.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DialogFooterButtonsEnum } from 'src/app/dialogs/dialog-footer/dialog-footer.component';
import { MessageDialogIcon } from 'src/app/dialogs/message-dialog/message-dialog.component';

@Component({
    selector: 'app-loadflow-data-transport-models',
    templateUrl: './loadflow-data-transport-models.component.html',
    styleUrls: ['./loadflow-data-transport-models.component.css']
})
export class LoadflowDataTransportModelsComponent extends DataTableBaseComponent<TransportModelEntry> {

    constructor(
        private dataService: LoadflowDataService,
        private dialogService: DialogService,
        private datasetsService: DatasetsService
        ) {
        super()
        this.dataFilter.sort = { active: 'generatorType', direction: 'asc'};
        this.data  = new MatTableDataSource()

        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.transportModels = results.transportModels.data
            this.transportModelEntries = results.transportModelEntries
            let sm: TransportModel | undefined
            // need to see if currently selected model still exists
            sm = this.transportModels.find(m=>m.id == this.selectedModel?.id)
            // select a new model (means the dataset has changed)
            if ( !sm ) {
                // else first one in the list
                sm = this.transportModels.length>0 ? this.transportModels[0] : undefined
            }
            this.selectModel(sm)
        }))
    }

    transportModels: TransportModel[] = []
    transportModelEntries: DatasetData<TransportModelEntry> | undefined
    selectedModel: TransportModel | undefined
    displayedColumns:string[] = ['generatorType','totalCapacity','autoScaling','scaling']
    generatorTypeEnum = GeneratorType

    selectModel( tm: TransportModel | undefined ) {
        this.selectedModel = tm
        if ( this.selectedModel && this.transportModelEntries ) {
            let tmEntries = this.getEntries(this.selectedModel,this.transportModelEntries)
            this.createDataSource(this.dataService.dataset,tmEntries);
        }
    }

    getEntries(tm: TransportModel, allEntries: DatasetData<TransportModelEntry>):DatasetData<TransportModelEntry> {
        let filteredEntries:DatasetData<TransportModelEntry> = {tableName: "TransportModelEntry", data: [], deletedData: [], userEdits: []}
        filteredEntries.data = allEntries.data.filter( m=>m.transportModelId == tm.id)
        filteredEntries.deletedData = allEntries.deletedData.filter( m=>m.transportModelId == tm.id)
        filteredEntries.userEdits = allEntries.userEdits
        return filteredEntries
    }

    getModelClass(tm: TransportModel) {
        let className = "modelSelector"
        className+= tm===this.selectedModel ? " selected" : ""
        return className
    }

    add() {
        if ( this.datasetsService.currentDataset?.isReadOnly) {
            this.dialogService.showMessageDialog(
            {
                message: `Cannot add a generator model as not owner of dataset`,
                icon: MessageDialogIcon.Info,
                buttons: DialogFooterButtonsEnum.Close
            })
        } else {
            this.dialogService.showLoadflowTransportModelDialog(undefined, (resp: DatasetData<any>[] | undefined) => {
                if (resp) {
                    // this will be the newly created transport model
                    let sm = resp.find(m => m.tableName === "TransportModel")?.data[0]
                    if (sm) {
                        this.selectModel(sm)
                    }
                }
            });
        }
    }

    isEditable(tm: TransportModel):boolean {
        return this.datasetsService.isEditable && ( tm.datasetId === this.dataService.dataset.id )
    }

    edit(tm: TransportModel) {
        this.dialogService.showLoadflowTransportModelDialog(tm,(resp: DatasetData<any>[] | undefined)=>{
            if ( resp ) {
                // this will be the newly created transport model
                let sm = resp.find(m=>m.tableName==="TransportModel")?.data[0]
                if ( sm ) {
                    this.selectModel(sm)
                }
            }
        });
    }

    delete(tm: TransportModel) {
        this.datasetsService.deleteItemWithCheck(tm.id,"TransportModel")
    }

    get disableDelete():boolean {

        return false
    }


}
