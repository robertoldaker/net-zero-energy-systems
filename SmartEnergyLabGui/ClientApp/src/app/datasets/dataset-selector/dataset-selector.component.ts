import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { Dataset, DatasetType } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogFooterButtonsEnum } from 'src/app/dialogs/dialog-footer/dialog-footer.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { MessageDialogIcon } from 'src/app/dialogs/message-dialog/message-dialog.component';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';

@Component({
    selector: 'app-dataset-selector',
    templateUrl: './dataset-selector.component.html',
    styleUrls: ['./dataset-selector.component.css']
})
export class DatasetSelectorComponent implements OnInit {

    constructor(private dataClientService: DataClientService, 
        private cookieService: CookieService,
        private dialogService: DialogService,
        private messageService: ShowMessageService
        ) { 
        
    }

    ngOnInit(): void {
        this.loadDatasets(0);
    }

    @Output()
    onSelected: EventEmitter<Dataset> = new EventEmitter<Dataset>()

    @Input()
    datasetType: DatasetType = DatasetType.Elsi
    dataset: Dataset | undefined
    datasetInfo: DatasetInfo[] = []

    getCookieName(): string {
        return `DatasetId_${this.datasetType}`
    } 

    loadDatasets(id: number) {
        this.dataClientService.Datasets(this.datasetType, (data)=>{
                this.setDatasets(data)
                if ( id==0) {
                    let savedDatasetStr = this.cookieService.get(this.getCookieName());
                    let savedDatasetId = savedDatasetStr ? parseInt(savedDatasetStr) : NaN
                    if ( !isNaN(savedDatasetId) ) {
                        id = savedDatasetId;
                    }    
                }
                this.setDataset(id)
            }
        )
    }
    
    setDataset(datasetId: number) {
        let di = this.datasetInfo.find(m=>m.dataset.id == datasetId);
        this.dataset = di ? di.dataset : undefined
        if ( !this.dataset && this.datasetInfo.length>0 ) {
            if ( this.datasetInfo.length>1) {
                this.dataset = this.datasetInfo[1].dataset
            } else {
                this.dataset = this.datasetInfo[0].dataset
            }
            datasetId = this.dataset.id;
        }
        this.cookieService.set(this.getCookieName(), datasetId.toString());
        //
        this.onSelected.emit(this.dataset)
    }

    private setDatasets(datasets: Dataset[]) {
        let datasetInfo:DatasetInfo[] = []
        let parent = datasets.find(m=>!m.parent);
        if ( parent) {
            addChildren(parent,0)
        }
        function addChildren(parent:Dataset, indent: number):Dataset[] {
            datasetInfo.push({indent: indent,dataset: parent})
            let children = datasets.filter(m=>m.parent?.id===parent?.id)
            children.forEach(m=>addChildren(m, indent+1))
            return children
        }
        this.datasetInfo = datasetInfo;
    } 


    addDataset() {
        if ( this.dataset ) {
            let parent = this.dataset
            this.dialogService.showDatasetDialog(null, parent, (datasetId)=>{
                if ( datasetId ) {
                    this.loadDatasets(datasetId)
                }
            })    
        }
    }

    editDataset() {
        if ( this.dataset ) {
            this.dialogService.showDatasetDialog(this.dataset, this.dataset.parent, (datasetId)=>{
                if ( datasetId ) {
                    this.loadDatasets(datasetId)
                }
            })
        }
    }

    deleteDataset() {
        if ( this.dataset) {
            this.dialogService.showMessageDialog({
                message: `Are you sure you wish to delete the dataset <b>${this.dataset.name}</b>?`,
                icon: MessageDialogIcon.Info,
                buttons: DialogFooterButtonsEnum.OKCancel
                }, ()=>{
                    if ( this.dataset) {
                        this.dataClientService.DeleteDataset(this.dataset.id, ()=>{
                            this.messageService.showMessageWithTimeout("Dataset successfully deleted")
                            this.loadDatasets(0)
                        })
                    }
                })
        }
    }    
    
}

export interface DatasetInfo {
    indent: number
    dataset: Dataset
}
