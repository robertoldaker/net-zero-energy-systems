import { Component, OnInit } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { DatasetData, GeneratorType, GenerationModel, GenerationModelEntry } from 'src/app/data/app.data';
import { MatTableDataSource } from '@angular/material/table';
import { DataTableBaseComponent } from 'src/app/datasets/data-table-base/data-table-base.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DialogFooterButtonsEnum } from 'src/app/dialogs/dialog-footer/dialog-footer.component';
import { MessageDialogIcon } from 'src/app/dialogs/message-dialog/message-dialog.component';

@Component({
    selector: 'app-loadflow-data-generation-models',
    templateUrl: './loadflow-data-generation-models.component.html',
    styleUrls: ['./loadflow-data-generation-models.component.css']
})
export class LoadflowDataGenerationModelsComponent extends DataTableBaseComponent<GenerationModelEntry> {

    constructor(
        private dataService: LoadflowDataService,
        private dialogService: DialogService,
        private datasetsService: DatasetsService
        ) {
        super()
        this.dataFilter.sort = { active: 'generatorType', direction: 'asc'};
        this.data  = new MatTableDataSource()

        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.generationModels = results.generationModels.data
            this.generationModelEntries = results.generationModelEntries
            let sm: GenerationModel | undefined
            // need to see if currently selected model still exists
            sm = this.generationModels.find(m=>m.id == this.selectedModel?.id)
            // select a new model (means the dataset has changed)
            if ( !sm ) {
                // else first one in the list
                sm = this.generationModels.length>0 ? this.generationModels[0] : undefined
            }
            this.selectModel(sm)
        }))
    }

    generationModels: GenerationModel[] = []
    generationModelEntries: DatasetData<GenerationModelEntry> | undefined
    selectedModel: GenerationModel | undefined
    displayedColumns:string[] = ['generatorType','totalCapacity','autoScaling','scaling']
    generatorTypeEnum = GeneratorType

    selectModel( tm: GenerationModel | undefined ) {
        this.selectedModel = tm
        if ( this.selectedModel && this.generationModelEntries ) {
            let tmEntries = this.getEntries(this.selectedModel,this.generationModelEntries)
            this.createDataSource(this.dataService.dataset,tmEntries);
        }
    }

    getEntries(tm: GenerationModel, allEntries: DatasetData<GenerationModelEntry>):DatasetData<GenerationModelEntry> {
        let filteredEntries:DatasetData<GenerationModelEntry> = {tableName: "GenerationModelEntry", data: [], deletedData: [], userEdits: []}
        filteredEntries.data = allEntries.data.filter( m=>m.generationModelId == tm.id)
        filteredEntries.deletedData = allEntries.deletedData.filter( m=>m.generationModelId == tm.id)
        filteredEntries.userEdits = allEntries.userEdits
        return filteredEntries
    }

    getModelClass(tm: GenerationModel) {
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
            this.dialogService.showLoadflowGenerationModelDialog(undefined, (resp: DatasetData<any>[] | undefined) => {
                if (resp) {
                    // this will be the newly created transport model
                    let sm = resp.find(m => m.tableName === "GenerationModel")?.data[0]
                    if (sm) {
                        this.selectModel(sm)
                    }
                }
            });
        }
    }

    isEditable(tm: GenerationModel):boolean {
        return this.datasetsService.isEditable && ( tm.datasetId === this.dataService.dataset?.id )
    }

    edit(tm: GenerationModel) {
        this.dialogService.showLoadflowGenerationModelDialog(tm,(resp: DatasetData<any>[] | undefined)=>{
            if ( resp ) {
                // this will be the newly created transport model
                let sm = resp.find(m=>m.tableName==="GenerationModel")?.data[0]
                if ( sm ) {
                    this.selectModel(sm)
                }
            }
        });
    }

    delete(tm: GenerationModel) {
        this.datasetsService.deleteItemWithCheck(tm.id,"GenerationModel")
    }

    get disableDelete():boolean {

        return false
    }


}
