import { HttpClient } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { PrimarySubstation, DistributionSubstation, GeographicalArea, SubstationLoadProfile, SubstationClassification, ClassificationToolInput, ClassificationToolOutput, LoadProfileSource, SubstationParams, VehicleChargingStation, SubstationChargingParams, SubstationHeatingParams, LoadflowResults, Boundary, NetworkData, ElsiScenario, ElsiDayResult, NewUser, Logon, User, ChangePassword, ElsiDataVersion, NewElsiDataVersion, ElsiGenParameter, ElsiGenCapacity, ElsiUserEdit, DatasetInfo, ElsiResult, GridSupplyPoint } from './app.data';
import { ShowMessageService } from '../main/show-message/show-message.service';
import { SignalRService } from '../main/signal-r-status/signal-r.service';

@Injectable({
    providedIn: 'root',
})

export class DataClientService {
    constructor(
        private http: HttpClient, 
        private showMessageService: ShowMessageService,
        private signalRService: SignalRService,
        @Inject('DATA_URL') private baseUrl: string) {
    }

    /**
     * Substations
     */
    GetPrimarySubstationsByGeographicalAreaId(gaId: number, onLoad: ((pss: PrimarySubstation[]) => void) | undefined) {
        this.http.get<PrimarySubstation[]>(this.baseUrl + `/Substations/PrimarySubstationsByGeographicalAreaId?gaId=${gaId}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    GetPrimarySubstationsByGridSupplyPointId(gspId: number, onLoad: ((pss: PrimarySubstation[]) => void) | undefined) {
        this.http.get<PrimarySubstation[]>(this.baseUrl + `/Substations/PrimarySubstationsByGridSupplyPointId?gspId=${gspId}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    GetDistributionSubstations(primaryId: number, onLoad: (dss: DistributionSubstation[]) => void | undefined) {
        this.http.get<DistributionSubstation[]>(this.baseUrl + `/Substations/DistributionSubstations?primaryId=${primaryId}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    GetDistributionSubstation(id: number, onLoad: (dss: DistributionSubstation) => void | undefined) {
        this.http.get<DistributionSubstation>(this.baseUrl + `/Substations/DistributionSubstation?id=${id}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    Search(str: string, maxResults: number, onLoad: (dsss: DistributionSubstation[]) => void | undefined) {
        this.http.get<DistributionSubstation[]>(this.baseUrl + '/Substations/Search',{ params: { str: str, maxResults: maxResults }}).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  {}); // Don;t report errors since it seems having all spaces reports an error??
    }

    SetSubstationParams(id: number, sParams: SubstationParams, onSave: () => void | undefined) {
        this.http.put(this.baseUrl + `/Substations/SetSubstationParams?id=${id}`,sParams).subscribe(() => {
            if (onSave !== undefined) {
                onSave();
            }
        }, error =>  this.logErrorMessage(error));
    }

    SetSubstationChargingParams(id: number, sParams: SubstationChargingParams, onSave: () => void | undefined) {
        this.http.put(this.baseUrl + `/Substations/SetSubstationChargingParams?id=${id}`,sParams).subscribe(() => {
            if (onSave !== undefined) {
                onSave();
            }
        }, error =>  this.logErrorMessage(error));
    }

    SetSubstationHeatingParams(id: number, sParams: SubstationHeatingParams, onSave: () => void | undefined) {
        this.http.put(this.baseUrl + `/Substations/SetSubstationHeatingParams?id=${id}`,sParams).subscribe(() => {
            if (onSave !== undefined) {
                onSave();
            }
        }, error =>  this.logErrorMessage(error));
    }

    /**
     * Supply points
     */
    GetGridSupplyPoints(gaId: number, onLoad: ((gsp: GridSupplyPoint[]) => void) | undefined) {
        this.http.get<GridSupplyPoint[]>(this.baseUrl + `/SupplyPoints/GridSupplyPoints?gaId=${gaId}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }


    /**
     * Vehicle charging
     */
    GetVehicleChargingStations(gaId: number, onLoad: (chargingStations: VehicleChargingStation[])=> void | undefined) {
        this.http.get<VehicleChargingStation[]>(this.baseUrl + `/VehicleCharging/VehicleChargingStations?gaId=${gaId}`).subscribe( result => {
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => this.logErrorMessage(error));
    }

    /**
     * Substation load profiles
     */
    GetDistributionSubstationLoadProfiles(id: number, source: LoadProfileSource, year: number, onLoad: (loadProfiles: SubstationLoadProfile[]) => void | undefined) {
        this.http.get<SubstationLoadProfile[]>(this.baseUrl + `/LoadProfiles/DistributionSubstationLoadProfiles?id=${id}&source=${source}&year=${year}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    GetPrimarySubstationLoadProfiles(id: number, source: LoadProfileSource, year: number, onLoad: (loadProfiles: SubstationLoadProfile[]) => void | undefined) {
        this.http.get<SubstationLoadProfile[]>(this.baseUrl + `/LoadProfiles/PrimarySubstationLoadProfiles?id=${id}&source=${source}&year=${year}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    GetGeographicalAreaLoadProfiles(id: number, source: LoadProfileSource, year: number, onLoad: (loadProfiles: SubstationLoadProfile[]) => void | undefined) {
        this.http.get<SubstationLoadProfile[]>(this.baseUrl + `/Loadprofiles/GeographicalAreaLoadProfiles?id=${id}&source=${source}&year=${year}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    /**
     * Substation classifications
     */
    GetDistributionSubstationClassifications(id: number, onLoad: (classifications: SubstationClassification[]) => void | undefined) {
        this.http.get<SubstationClassification[]>(this.baseUrl + `/Classifications/DistributionSubstationClassifications?id=${id}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    GetPrimarySubstationClassifications(id: number, aggregateResults: boolean, onLoad: (classifications: SubstationClassification[]) => void | undefined) {
        this.http.get<SubstationClassification[]>(this.baseUrl + `/Classifications/PrimarySubstationClassifications?id=${id}&aggregateResults=${aggregateResults}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    GetGeographicalAreaClassifications(id: number, aggregateResults: boolean, onLoad: (classifications: SubstationClassification[]) => void | undefined) {
        this.showMessageService.showMessage("Loading geo classifications ...")
        this.http.get<SubstationClassification[]>(this.baseUrl + `/Classifications/GeographicalAreaClassifications?id=${id}&aggregateResults=${aggregateResults}`).subscribe(result => {
            this.showMessageService.clearMessage()
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    /**
     * Organisations
     */
     GetGeographicalArea(areaName: string, onLoad: (ga: GeographicalArea) => void | undefined) {
        this.http.get<GeographicalArea>(this.baseUrl + `/Organisations/GeographicalArea?areaName=${areaName}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    /**
     * Classification tool
     */
    RunClassificationTool(input: ClassificationToolInput, onLoad: (output: ClassificationToolOutput) => void | undefined) {
        this.http.post<ClassificationToolOutput>(this.baseUrl + '/ClassificationTool/Run', input).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error => { this.logErrorMessage(error) });
    }

    RunClassificationToolOnSubstation(id: number, onLoad?: () => void) {
        this.showMessageService.showMessage("Calculating ...");
        this.http.post(this.baseUrl + `/ClassificationTool/RunOnSubstation?id=${id}`,{}).subscribe(result => {
            this.showMessageService.clearMessage();
            if (onLoad !== undefined) {
                onLoad();
            }
        }, error => { this.showMessageService.clearMessage(); this.logErrorMessage(error) });
    }

    RunClassificationToolAll(gaId: number, input: ClassificationToolInput, onLoad?: () => void) {
        this.http.post<ClassificationToolOutput>(this.baseUrl + `/ClassificationTool/RunAllAsync?gaId=${gaId}`, input).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad();
            }
        }, error => { this.logErrorMessage(error) });
    }


    /**
     *  Loadflow 
     */
    GetNetworkData( onLoad: (networkData: NetworkData)=> void | undefined) {
        this.http.get<NetworkData>(this.baseUrl + `/Loadflow/NetworkData`).subscribe( result => {
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => this.logErrorMessage(error));        
    }

    GetBoundaries( onLoad: (boundaries: Boundary[])=> void | undefined) {
        this.http.get<Boundary[]>(this.baseUrl + `/Loadflow/Boundaries`).subscribe( result => {
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => this.logErrorMessage(error));        
    }

    RunBaseLoadflow( onLoad: (results: LoadflowResults)=> void | undefined) {
        this.showMessageService.showMessage("Calculating ...");
        this.http.post<LoadflowResults>(this.baseUrl + `/Loadflow/RunBaseLoadflow`,{}).subscribe( result => {
            this.showMessageService.clearMessage()
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => { this.showMessageService.clearMessage(); this.logErrorMessage(error)} );        
    }

    SetBound( boundaryName:string, onLoad: (results: LoadflowResults)=> void | undefined) {
        this.showMessageService.showMessage("Calculating ...");
        this.http.post<LoadflowResults>(this.baseUrl + `/Loadflow/SetBound?boundaryName=${boundaryName}`,{}).subscribe( result => {
            this.showMessageService.clearMessage()
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => { this.showMessageService.clearMessage(); this.logErrorMessage(error)} );        
    }

    RunBoundaryTrip( boundaryName: string, tripName: string, onLoad: (results: LoadflowResults)=> void | undefined) {
        this.showMessageService.showMessage("Calculating ...");
        this.http.post<LoadflowResults>(this.baseUrl + `/Loadflow/RunBoundaryTrip?boundaryName=${boundaryName}&tripName=${tripName}`,{}).subscribe( result => {
            this.showMessageService.clearMessage()
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => { this.showMessageService.clearMessage(); this.logErrorMessage(error)} );        
    }

    RunAllBoundaryTrips( boundaryName: string,  onLoad: (results: LoadflowResults)=> void | undefined) {
        let connectionId = this.signalRService.hubConnection?.connectionId;
        this.showMessageService.showMessage("Calculating ...");
        this.http.post<LoadflowResults>(this.baseUrl + `/Loadflow/RunAllBoundaryTrips?boundaryName=${boundaryName}&connectionId=${connectionId}`,{}).subscribe( result => {
            this.showMessageService.clearMessage()
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => { this.showMessageService.clearMessage(); this.logErrorMessage(error)} );        
    }
    /**
     * Elsi
     */    
    RunSingleDay(day: number, scenario: ElsiScenario, datasetId: number, onLoad: (results: ElsiDayResult)=> void | undefined) {
        let connectionId = this.signalRService.hubConnection?.connectionId;
        this.getRequestWithMessage<ElsiDayResult>(
            "Calculating ...",
            `/Elsi/RunSingleDay?day=${day}&scenario=${scenario}&datasetId=${datasetId}&connectionId=${connectionId}`,
            onLoad
        )     
    }

    RunDays(startDay: number, endDay: number, scenario: ElsiScenario, datasetId: number,  onLoad: (results: string)=> void | undefined) {
        let connectionId = this.signalRService.hubConnection?.connectionId;
        this.getRequestWithMessage<string>(
            'Starting calculation ...', 
            `/Elsi/RunDays?startDay=${startDay}&endDay=${endDay}&scenario=${scenario}&datasetId=${datasetId}&connectionId=${connectionId}`,
            onLoad);

    }

    ElsiDataVersions(onLoad: (results: ElsiDataVersion[])=> void | undefined) {
        this.getRequest<ElsiDataVersion[]>(
            `/Elsi/DataVersions`,
            onLoad);
    }

    NewElsiDataVersion(data: NewElsiDataVersion, onOk: (resp: string)=> void | undefined, onError: (error: any)=>void | undefined) {
        this.postDialogRequest<NewElsiDataVersion>('/Elsi/NewDataVersion', data, onOk, onError);
    }

    SaveElsiDataVersion(data: ElsiDataVersion, onOk: (resp: string)=> void | undefined, onError: (error: any)=>void | undefined) {
        this.postDialogRequest<ElsiDataVersion>('/Elsi/SaveDataVersion', data, onOk, onError);
    }

    DeleteElsiDataVersion(id: number, onLoad: (resp: string)=> void | undefined) {
        this.postRequest<number>('/Elsi/DeleteDataVersion', id, onLoad);
    }

    ElsiGenParameters(versionId: number, onLoad: (results: ElsiGenParameter[])=> void | undefined) {
        this.getRequest<ElsiGenParameter[]>(
            `/Elsi/GenParameters?versionId=${versionId}`,
            onLoad);
    }

    ElsiGenCapacities(versionId: number, onLoad: (results: ElsiGenCapacity[])=> void | undefined) {
        this.getRequest<ElsiGenCapacity[]>(
            `/Elsi/GenCapacities?versionId=${versionId}`,
            onLoad);
    }

    ElsiDatasetInfo(versionId: number, onLoad: (results: DatasetInfo)=> void | undefined) {
        this.getRequestWithMessage<DatasetInfo>('Loading ...',
            `/Elsi/DatasetInfo?versionId=${versionId}`,
            onLoad);
    }

    SaveElsiUserEdit(userEdit: ElsiUserEdit, onSave: (resp: string)=>void|undefined) {
        this.postRequest('/Elsi/SaveUserEdit',userEdit,onSave);
    }

    DeleteUserEdit(id: number, onOk: (resp: string)=>void|undefined) {
        this.postRequest('/Elsi/DeleteUserEdit',id, onOk);
    }

    ElsiResults(datasetId: number, scenario: ElsiScenario, onLoad: (results: ElsiResult[])=> void | undefined) {
        this.getRequest<ElsiResult[]>(
            `/Elsi/Results?datasetId=${datasetId}&scenario=${scenario}`,
            onLoad);
    }

    ElsiResultCount(datasetId: number, onLoad: (count: number)=> void | undefined) {
        this.getRequest<number>(
            `/Elsi/ResultCount?datasetId=${datasetId}`,
            onLoad);
    }

    ElsiDayResult(elsiResultId: number, onLoad: (results: ElsiDayResult)=> void | undefined) {
        this.getRequest<ElsiDayResult>(
            `/Elsi/DayResult?elsiResultId=${elsiResultId}`,
            onLoad);
    }
    
    /* users */
    SaveNewUser(newUser: NewUser, onOk: (resp: string)=> void | undefined, onError: (error: any)=>void | undefined) {
        this.postDialogRequest<NewUser>('/Users/SaveNewUser', newUser, onOk, onError);
    }

    Logon(logon: Logon, onOk: (resp: string)=> void | undefined, onError: (error: any)=>void | undefined) {
        this.postDialogRequest<Logon>('/Users/Logon', logon, onOk, onError);
    }

    Logoff(onOk: (resp: string)=> void | undefined,) {
        this.postRequest<void>('/Users/Logoff', undefined, onOk);
    }

    CurrentUser(onLoad: (resp: User | undefined)=> void | undefined) {
        this.getRequest<User | undefined>('/Users/CurrentUser', onLoad);
    }

    ChangePassword(changePassword: ChangePassword, onLoad: (resp: string)=> void | undefined, onError: (error: any)=>void | undefined) {
        this.postDialogRequest<ChangePassword>('/Users/ChangePassword', changePassword, onLoad, onError);
    }

    GetUsers(onLoad: (resp: User[])=> void | undefined) {
        this.getRequest<User[]>('/Users/Users', onLoad);
    }

    /* Admin */
    BackupDb(onComplete: (resp: any)=> void | undefined) {
        this.getBasicRequest('/Admin/BackupDb', onComplete);
    }

    Logs(onComplete: (resp: string)=> void | undefined) {
        this.getRequest<string>('/Admin/Logs', onComplete);
    }

    CancelBackgroundTask(taskId: number, onComplete: (resp: any)=> void | undefined) {
        this.getBasicRequest(`/Admin/Cancel?taskId=${taskId}`, onComplete)
    }



    /* shared */
    private getBasicRequest(url: string, onLoad: (resp: any)=>void | undefined) {
        this.http.get(this.baseUrl + url).subscribe(resp => {
            if ( onLoad) {
                onLoad(resp);
            }
        },resp => { 
            this.logErrorMessage(resp);
        })
    }

    private getRequest<T>(url: string, onLoad: (resp: T)=>void | undefined) {
        this.http.get<T>(this.baseUrl + url).subscribe(resp => {
            if ( onLoad) {
                onLoad(resp);
            }
        },resp => { 
            this.logErrorMessage(resp);
        })
    }

    private getRequestWithMessage<T>(message: string, url: string, onLoad: (resp: T)=>void | undefined) {
        this.showMessageService.showMessage(message);
        this.http.get<T>(this.baseUrl + url).subscribe(resp => {
            this.showMessageService.clearMessage()
            if ( onLoad) {
                onLoad(resp);
            }
        },resp => { 
            this.showMessageService.clearMessage()
            this.logErrorMessage(resp);
        })
    }

    private postRequest<T>(url: string, data: T,onOk: (resp: string)=>void | undefined) {
        this.http.post<string>(this.baseUrl + url, data).subscribe(resp => {
            if ( onOk) {
                onOk(resp);
            }
        },resp => { 
            this.logErrorMessage(resp);
        })
    }

    private postRequestWithMessage<T>(message: string, url: string, data: T,onOk: (resp: string)=>void | undefined) {
        this.showMessageService.showMessage(message);
        this.http.post<string>(this.baseUrl + url, data).subscribe(resp => {
            this.showMessageService.clearMessage()
            if ( onOk) {
                onOk(resp);
            }
        },resp => { 
            this.showMessageService.clearMessage()
            this.logErrorMessage(resp);
        })
    }

    private postDialogRequest<T>(url: string, data: T,onOk: (resp: string)=>void | undefined, onError: (error: any)=> void | undefined) {
        this.http.post<string>(this.baseUrl + url, data).subscribe(resp => {
            if ( onOk) {
                onOk(resp);
            }
        },resp => { 
            if ( onError && resp.status == 422) { 
                onError(resp.error) 
            } else {
                this.logErrorMessage(resp);
            }
        })
    }

    private logErrorMessage(error:any) {
        let message:string = error.message;
        if ( typeof error.error === 'string') {
            message += '\n\n' + error.error;
        }        
        this.showMessageService.showModalErrorMessage(message)
    }



}