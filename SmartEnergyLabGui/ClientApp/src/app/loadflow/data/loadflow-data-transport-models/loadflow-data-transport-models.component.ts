import { Component, OnInit } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { DatasetData, GeneratorType, TransportModel, TransportModelEntry } from 'src/app/data/app.data';
import { MatTableDataSource } from '@angular/material/table';
import { DataTableBaseComponent } from 'src/app/datasets/data-table-base/data-table-base.component';

@Component({
    selector: 'app-loadflow-data-transport-models',
    templateUrl: './loadflow-data-transport-models.component.html',
    styleUrls: ['./loadflow-data-transport-models.component.css']
})
export class LoadflowDataTransportModelsComponent extends DataTableBaseComponent<TransportModelEntry> {

    constructor(private dataService: LoadflowDataService) {
        super()
        this.dataFilter.sort = { active: 'generatorType', direction: 'asc'};
        this.data  = new MatTableDataSource()

        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.transportModels = results.transportModels.data
            this.transportModelEntries = results.transportModelEntries
            // need to see if currently selected model still exists
            let sm = this.transportModels.find(m=>m.id == this.selectedModel?.id)
            // select a new model (means the dataset has changed)
            if ( !sm ) {
                if ( this.dataService.transportModel  ) {
                    // choose currently selected one??
                    sm = this.transportModels.find(m=>m.id == this.dataService.transportModel?.id)
                } else {
                    // else first one in the list
                    sm = this.transportModels.length>0 ? this.transportModels[0] : undefined
                }
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

}
