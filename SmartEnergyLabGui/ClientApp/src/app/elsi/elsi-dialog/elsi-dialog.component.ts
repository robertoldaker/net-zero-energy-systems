import { Component, OnInit } from '@angular/core';
import { ElsiDataVersion } from 'src/app/data/app.data';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { MessageDialogIcon } from 'src/app/dialogs/message-dialog/message-dialog.component';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { ElsiDataService } from '../elsi-data.service';
import { DialogFooterButtonsEnum } from 'src/app/dialogs/dialog-footer/dialog-footer.component';

@Component({
    selector: 'app-elsi-dialog',
    templateUrl: './elsi-dialog.component.html',
    styleUrls: ['./elsi-dialog.component.css']
})

export class ElsiDialogComponent extends ComponentBase implements OnInit {

    constructor(public service: ElsiDataService, private dialogService: DialogService, private messageService: ShowMessageService) { 
        super()
        this.date = new Date(2023,0,1)
        this.startDate = new Date(2023,0,2)
        this.endDate = new Date(2023,0,6)
        this.percentComplete = 0
        this.progressText = "";
        this.numDaysDone = 0;
        this.numDaysToDo = 0;
        this.datasets = [];

        //
        this.addSub(this.service.Progress.subscribe((e)=>{
            this.numDaysDone++;
            this.percentComplete = 100*this.numDaysDone/this.numDaysToDo;
            this.progressText=this.getProgressText();
        }))
        //
        this.addSub(this.service.DatasetsChange.subscribe((dataVersions)=>{
            this.setDatasets(dataVersions)
        }))

    }

    private numDaysDone: number;
    private numDaysToDo: number;
    //
    percentComplete: number
    progressText: string

    date: Date
    startDate: Date
    endDate: Date

    ngOnInit(): void {
    }

    scenarioChanged(e:number) {
        this.service.setScenario(e);
    }

    getProgressText() {
        return `${this.numDaysDone} of ${this.numDaysToDo} complete`;
    }

    private daysIntoYear(date:Date){
        return (Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()) - Date.UTC(date.getFullYear(), 0, 0)) / 24 / 60 / 60 / 1000;
    }

    runSingleDay() {
        this.percentComplete = 0;
        this.numDaysToDo = 1;
        this.numDaysDone = 0;
        this.progressText=this.getProgressText();
        let day = this.daysIntoYear(this.date)
        this.service.runDays(day,day);
    }

    runDays() {
        this.percentComplete = 0;
        let startDay = this.daysIntoYear(this.startDate);
        let endDay = this.daysIntoYear(this.endDate);
        this.numDaysToDo = endDay - startDay + 1;
        this.numDaysDone = 0;
        this.progressText=this.getProgressText();
        this.service.runDays(startDay,endDay);
    }

    runYear() {
        this.percentComplete = 0;
        let startDay = 1
        let endDay = 365
        this.numDaysToDo = endDay - startDay + 1;
        this.numDaysDone = 0;
        this.progressText=this.getProgressText();
        this.service.runDays(startDay,endDay);
    }

    get canRun() {
        return this.service.canRun;
    }

    addDataset() {
        this.dialogService.showElsiDatasetDialog(null)
    }

    editDataset() {
        if ( this.service.dataset ) {
            this.dialogService.showElsiDatasetDialog(this.service.dataset)
        }
    }

    deleteDataset() {
        if ( this.service.dataset) {
            this.dialogService.showMessageDialog({
                message: `Are you sure you wish to delete the dataset <b>${this.service.dataset.name}</b>?`,
                icon: MessageDialogIcon.Info,
                buttons: DialogFooterButtonsEnum.OKCancel
                }, ()=>{
                    if ( this.service.dataset) {
                        this.service.deleteDataset(this.service.dataset, ()=>{
                            this.messageService.showMessageWithTimeout("Dataset successfully deleted")
                        })
                    }
                })
        }
    }

    datasets:DatasetInfo[]

    private setDatasets(dataVersions: ElsiDataVersion[]) {
        let datasets:DatasetInfo[] = []
        let parent = this.service.dataVersions.find(m=>!m.parent);
        if ( parent) {
            addChildren(parent,0)
        }
        function addChildren(parent:ElsiDataVersion, indent: number):ElsiDataVersion[] {
            datasets.push({indent: indent,dataset: parent})
            let children = dataVersions.filter(m=>m.parent?.id===parent?.id)
            children.forEach(m=>addChildren(m, indent+1))
            return children
        }
        this.datasets = datasets;
    }    
}

export interface DatasetInfo {
    indent: number
    dataset: ElsiDataVersion
}
