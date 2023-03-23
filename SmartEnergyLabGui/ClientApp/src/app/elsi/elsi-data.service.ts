import { EventEmitter, Injectable, OnDestroy } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { DatasetInfo, ElsiDataVersion, ElsiDayResult, ElsiProgress, ElsiResult, ElsiScenario, ElsiUserEdit } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';
import { DialogService } from '../dialogs/dialog.service';
import { MessageDialogIcon } from '../dialogs/message-dialog/message-dialog.component';
import { SignalRService } from '../main/signal-r-status/signal-r.service';
import { UserService } from '../users/user.service';
import { ServiceBase } from '../utils/service-base';

@Injectable({
    providedIn: 'root'
})
export class ElsiDataService extends ServiceBase {

    constructor(        
        private dataClientService: DataClientService, 
        private signalRService: SignalRService, 
        private cookieService: CookieService,
        private dialogService: DialogService,
        private userService: UserService) {

            super()
            console.log('ElsiDataService contructor')
            this.scenario = ElsiScenario.SteadyProgression
            this.setSavedScenario()
            this.dataVersions = []
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
            this.loadDataVersions(0);
            //
            this.addSub(this.userService.LogonEvent.subscribe((user)=>{
                this.loadDataVersions(0);
            }));
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
    dataset: ElsiDataVersion | undefined
    dataVersions: ElsiDataVersion[]    
    setDataset(datasetId: number) {
        this.dataset = this.dataVersions.find(m=>m.id == datasetId);
        if ( !this.dataset && this.dataVersions.length>0) {
            this.dataset = this.dataVersions[0]
            datasetId = this.dataset.id;
        }
        this.cookieService.set('ElsiDatasetId', datasetId.toString());
        //
        this.loadDataset();
    }
    deleteDataset(dataset: ElsiDataVersion, onDelete: ()=>void) {
        this.dataClientService.DeleteElsiDataVersion(dataset.id,()=>{
            this.dataset = undefined
            this.loadDataVersions(0)
            onDelete()
        });      
    }

    loadDataVersions(id: number) {
        this.dataClientService.ElsiDataVersions((data)=>{
            this.dataVersions = data
            if ( id==0 ) {
                let savedDatasetStr = this.cookieService.get('ElsiDatasetId');
                let savedDatasetId = savedDatasetStr ? parseInt(savedDatasetStr) : NaN
                if ( !isNaN(savedDatasetId) ) {
                    id = savedDatasetId;
                } 
            }
            this.setDataset(id)
            this.DatasetsChange.emit(this.dataVersions);
        })
    }

    loadDataset() {
        if ( this.dataset ) {
            this.dataClientService.ElsiDatasetInfo(this.dataset.id, (data)=>{
                this.datasetInfo = data
                this.DatasetInfoChange.emit(this.datasetInfo);
            })
            //
            this.loadResults();
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
        return this.signalRService.isConnected && !this.inRun
    }
    get isReadOnly():boolean {
        return !this.dataset?.parent || this.inRun;
    }

    // Elsi scenarios
    elsiScenarioInfo = new ElsiScenarioInfo()

    datasetInfo: DatasetInfo | undefined
    results: ElsiResult[] | undefined


    addUserEdit(userEdit: ElsiUserEdit) {
        if ( this.dataset) {
            this.dataClientService.ElsiResultCount(this.dataset.id,(count)=>{
                if ( count>0 && this.dataset) {
                    this.dialogService.showMessageDialog({
                        message: `This will delete <b>${count}</b> day result(s) associated with this dataset <b>${this.dataset.name}</b>. Continue?`,
                        icon: MessageDialogIcon.Info
                        }, ()=>{
                            this.saveUserEdit(userEdit);
                        })        
                } else {
                    this.saveUserEdit(userEdit);
                }
            })    
        }        
    }

    private saveUserEdit(userEdit: ElsiUserEdit) {
        this.dataClientService.SaveElsiUserEdit(userEdit, ()=>{
            this.loadDataset()
        })
    }

    removeUserEdit(userEditId: number) {
        if ( this.dataset) {
            this.dataClientService.ElsiResultCount(this.dataset.id,(count)=>{
                if ( count>0 && this.dataset) {
                    this.dialogService.showMessageDialog({
                        message: `This will delete <b>${count}</b> day result(s) associated with this dataset <b>${this.dataset.name}</b>. Continue?`,
                        icon: MessageDialogIcon.Info
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
        this.dataClientService.DeleteUserEdit(userEditId, ()=>{
            this.loadDataset()
        })
    }

    Progress:EventEmitter<ElsiProgress> = new EventEmitter<ElsiProgress>()
    LogMessageAvailable:EventEmitter<string> = new EventEmitter<string>()
    RunStart:EventEmitter<any> = new EventEmitter<any>()
    DatasetsChange:EventEmitter<ElsiDataVersion[]> = new EventEmitter<ElsiDataVersion[]>()
    DatasetInfoChange:EventEmitter<DatasetInfo> = new EventEmitter<DatasetInfo>()
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
