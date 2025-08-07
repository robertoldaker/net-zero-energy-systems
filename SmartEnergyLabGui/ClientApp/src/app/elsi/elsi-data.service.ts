import { EventEmitter, Injectable, OnDestroy } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { Dataset, DatasetType, ElsiDatasetInfo, ElsiProgress, ElsiResult, ElsiScenario, UserEdit } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';
import { DialogService } from '../dialogs/dialog.service';
import { MessageDialogIcon } from '../dialogs/message-dialog/message-dialog.component';
import { SignalRService } from '../main/signal-r-status/signal-r.service';
import { UserService } from '../users/user.service';
import { ServiceBase } from '../utils/service-base';
import { DialogFooterButtonsEnum } from '../dialogs/dialog-footer/dialog-footer.component';
import { DatasetsService } from '../datasets/datasets.service';

@Injectable({
    providedIn: 'root'
})
export class ElsiDataService extends ServiceBase {

    constructor(
        private dataClientService: DataClientService,
        private signalRService: SignalRService,
        private cookieService: CookieService,
        private dialogService: DialogService,
        private datasetsService: DatasetsService,
        private userService: UserService) {

        super()
        this.scenario = ElsiScenario.SteadyProgression
        this.setSavedScenario()
        this.inRun = false
        this.elsiProgress = { numComplete: 0, numToDo: 0, percentComplete: 0 }
        this.signalRService.hubConnection.on('Elsi_Progress', (e) => {
            this.elsiProgress = e
            this.inRun = (e.numComplete<e.numToDo)
            this.Progress.emit(e);
            if (!this.inRun) {
                this.loadResults();
            }
        })
        this.signalRService.hubConnection.on('Elsi_Log', (data) => {
            this.LogMessageAvailable.emit(data);
        })
        //
        this.addSub(this.datasetsService.AfterEdit.subscribe( (resp)=>{
            if (resp.type != DatasetType.Elsi) {
                return;
            }
            this.reloadDataset()
        }))
       //
    }

    private setSavedScenario() {
        let savedScenarioStr = this.cookieService.get('ElsiScenario');
        let savedScenario = savedScenarioStr ? parseInt(savedScenarioStr) : NaN
        if ( isNaN(savedScenario)) {
            this.scenario = ElsiScenario.SteadyProgression
        } else {
            this.scenario = savedScenario
        }
    }

    public runDays(startDay: number, endDay: number) {
        if ( this.dataset ) {
            this.RunStart.emit();
            this.dataClientService.RunDays(startDay,endDay, this.scenario, this.dataset.id, (result) => {
                this.inRun = true
                this.elsiProgress.numToDo = endDay - startDay + 1;
                this.elsiProgress.numComplete = 0;
            });
        }
    }

    scenario: ElsiScenario
    setScenario(scenario: ElsiScenario) {
        this.scenario = scenario;
        this.cookieService.set('ElsiScenario', scenario.toString());
        this.loadResults();
    }
    get dataset(): Dataset {
        let ds = this.datasetsService.currentDataset
        if ( ds?.type === DatasetType.Elsi) {
            return ds
        } else {
            return {id: 0, name: "?", type: DatasetType.Elsi, isReadOnly: true, parent: null }
        }
    }

    setDataset(dataset: Dataset) {
        //??this.dataset = dataset
        this.loadDataset(dataset);
    }

    loadDataset(dataset: Dataset) {
        this.dataClientService.ElsiDatasetInfo(dataset.id, (data)=>{
            this.datasetsService.setDataset(dataset)
            //
            this.datasetInfo = data
            this.DatasetInfoChange.emit(this.datasetInfo)
            //
            this.loadResults();
        })
    }

    reloadDataset() {
        if ( this.dataset ) {
            this.loadDataset(this.dataset)
        }
    }

    loadResults() {
        if ( this.dataset) {
            this.dataClientService.ElsiResults(this.dataset.id,this.scenario,(data)=>{
                this.results = data
                this.ResultsChange.emit(this.results);
            })
        }
    }

    private inRun: boolean
    private elsiProgress: ElsiProgress
    get canRun():boolean {
        return this.dataset?.parent!=null && this.signalRService.isConnected && !this.inRun
    }

    // Elsi scenarios
    elsiScenarioInfo = new ElsiScenarioInfo()

    datasetInfo: ElsiDatasetInfo | undefined
    results: ElsiResult[] | undefined

    addUserEdit(userEdit: UserEdit) {
        if ( this.dataset) {
            this.dataClientService.ElsiResultCount(this.dataset.id,(count)=>{
                if ( count>0 && this.dataset) {
                    this.dialogService.showMessageDialog({
                        message: `This will delete <b>${count}</b> day result(s) associated with this dataset <b>${this.dataset.name}</b>. Continue?`,
                        icon: MessageDialogIcon.Info,
                        buttons: DialogFooterButtonsEnum.OKCancel
                        }, ()=>{
                            this.saveUserEdit(userEdit);
                        })
                } else {
                    this.saveUserEdit(userEdit);
                }
            })
        }
    }

    private saveUserEdit(userEdit: UserEdit) {
        this.dataClientService.SaveElsiUserEdit(userEdit, ()=>{
            this.reloadDataset()
        })
    }

    removeUserEdit(userEditId: number) {
        if ( this.dataset) {
            this.dataClientService.ElsiResultCount(this.dataset.id,(count)=>{
                if ( count>0 && this.dataset) {
                    this.dialogService.showMessageDialog({
                        message: `This will delete <b>${count}</b> day result(s) associated with this dataset <b>${this.dataset.name}</b>. Continue?`,
                        icon: MessageDialogIcon.Info,
                        buttons: DialogFooterButtonsEnum.OKCancel
                        }, ()=>{
                            this.deleteUserEdit(userEditId);
                        })
                } else {
                    this.deleteUserEdit(userEditId);
                }
            })
        }
    }

    private deleteUserEdit(userEditId: number) {
        this.dataClientService.DeleteElsiUserEdit(userEditId, ()=>{
            this.reloadDataset()
        })
    }

    Progress:EventEmitter<ElsiProgress> = new EventEmitter<ElsiProgress>()
    LogMessageAvailable:EventEmitter<string> = new EventEmitter<string>()
    RunStart:EventEmitter<any> = new EventEmitter<any>()
    DatasetInfoChange:EventEmitter<ElsiDatasetInfo> = new EventEmitter<ElsiDatasetInfo>()
    ResultsChange:EventEmitter<ElsiResult[]> = new EventEmitter<ElsiResult[]>()

}

export class ElsiScenarioInfo {
    constructor() {
        this.map = new Map<ElsiScenario,string>()
        this.map.set(ElsiScenario.CommunityRenewables,"Community renewables")
        this.map.set(ElsiScenario.ConsumerEvolution,"Consumer evolution")
        this.map.set(ElsiScenario.SteadyProgression,"Steady progression")
        this.map.set(ElsiScenario.TwoDegrees,"Two degrees")
        this.keys = [];
        for (const [key, value] of Object.entries(ElsiScenario)) {
            if ( typeof value == 'number') {
                this.keys.push(value);
            }
        }
    }

    private map: Map<ElsiScenario,string>

    public get(scenario: ElsiScenario) {
        return this.map.get(scenario);
    }

    public keys:ElsiScenario[]
}
