import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { Dataset, DatasetType } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogFooterButtonsEnum } from 'src/app/dialogs/dialog-footer/dialog-footer.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { MessageDialogIcon } from 'src/app/dialogs/message-dialog/message-dialog.component';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';
import { UserService } from 'src/app/users/user.service';
import { DatasetsService } from '../datasets.service';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-dataset-selector',
    templateUrl: './dataset-selector.component.html',
    styleUrls: ['./dataset-selector.component.css']
})
export class DatasetSelectorComponent extends ComponentBase implements OnInit {

    constructor(private dataClientService: DataClientService,
        private cookieService: CookieService,
        private dialogService: DialogService,
        private messageService: ShowMessageService,
        public userService: UserService,
        public datasetsService: DatasetsService
        ) {
            super()
            this.addSub( datasetsService.SetDataset.subscribe( (ds)=>{
                // store dataset id in cookie for next re-start
                this.cookieService.set(this.getCookieName(), ds.id.toString());
            }) )

    }

    ngOnInit(): void {
        //
        this.datasetsService.reset()
        this.loadDatasets(0)
    }

    @Output()
    onSelected: EventEmitter<Dataset> = new EventEmitter<Dataset>()

    @Output()
    onReload: EventEmitter<Dataset> = new EventEmitter<Dataset>()

    @Input()
    datasetType: DatasetType = DatasetType.Elsi

    @Input()
    showQuickHelp: boolean = false

    get dataset(): Dataset | undefined {
        return this.datasetsService.currentDataset
    }
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
                this.datasetSelected(id)
            }
        )
    }

    datasetSelected(datasetId: number) {
        let di = this.datasetInfo.find(m=>m.dataset.id == datasetId);
        let dataset = di ? di.dataset : undefined
        if ( !dataset && this.datasetInfo.length>0 ) {
            if ( this.datasetInfo.length>1) {
                dataset = this.datasetInfo[1].dataset
            } else {
                dataset = this.datasetInfo[0].dataset
            }
            datasetId = dataset.id;
        }
        //
        if ( dataset ) {
            this.onSelected.emit(dataset)
        }
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
                        this.messageService.showMessage("Deleting dataset ...")
                        this.dataClientService.DeleteDataset(this.dataset.id, ()=>{
                            this.messageService.showMessageWithTimeout("Dataset successfully deleted")
                            this.loadDatasets(0)
                        })
                    }
                })
        }
    }

    reloadDataset() {
        if ( this.dataset ) {
            this.onReload.emit(this.dataset)
        }
    }

}

export interface DatasetInfo {
    indent: number
    dataset: Dataset
}
