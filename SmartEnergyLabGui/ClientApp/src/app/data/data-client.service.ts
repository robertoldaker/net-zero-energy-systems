import { HttpClient, HttpParams } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { PrimarySubstation, DistributionSubstation, GeographicalArea, SubstationLoadProfile, SubstationClassification, ClassificationToolInput, ClassificationToolOutput, LoadProfileSource, SubstationParams, VehicleChargingStation, SubstationChargingParams, SubstationHeatingParams, LoadflowResults, Boundary, NetworkData, ElsiScenario, ElsiDayResult, NewUser, Logon, User, ChangePassword, ElsiGenParameter, ElsiGenCapacity, UserEdit, ElsiDatasetInfo, ElsiResult, GridSupplyPoint, DataModel, GISBoundary, GridSubstation, LocationData, LoadNetworkDataSource, SubstationSearchResult, EVDemandStatus, SystemInfo, ILogs, ResetPassword, SolarInstallation, Dataset, NewDataset, DatasetType, DatasetData, EditItem } from './app.data';
import { ShowMessageService } from '../main/show-message/show-message.service';
import { SignalRService } from '../main/signal-r-status/signal-r.service';
import { DialogService } from '../dialogs/dialog.service';
import { MessageDialogIcon } from '../dialogs/message-dialog/message-dialog.component';
import { DialogFooterButtonsEnum } from '../dialogs/dialog-footer/dialog-footer.component';

@Injectable({
    providedIn: 'root',
})

export class DataClientService implements ILogs {
    constructor(
        private http: HttpClient, 
        private showMessageService: ShowMessageService,
        private signalRService: SignalRService,
        @Inject('DATA_URL') private baseUrl: string) {
    }

    /**
     * Substations
     */
    GetPrimarySubstation(id: number, onLoad: (pss: PrimarySubstation) => void) {
        this.http.get<PrimarySubstation>(this.baseUrl + `/Substations/PrimarySubstation?id=${id}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

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

    GetCustomersForPrimarySubstation(primaryId: number, onLoad: ((numCustomers: number) => void) | undefined) {
        this.http.get<number>(this.baseUrl + `/Substations/PrimarySubstation/Customers?primaryId=${primaryId}`).subscribe(result => {
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

    Search(str: string, maxResults: number, onLoad: (dsss: SubstationSearchResult[]) => void | undefined) {
        this.http.get<SubstationSearchResult[]>(this.baseUrl + '/Substations/Search',{ params: { str: str, maxResults: maxResults }}).subscribe(result => {
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
    GetAllGridSupplyPoints(onLoad: ((gsp: GridSupplyPoint[]) => void) | undefined) {
        this.http.get<GridSupplyPoint[]>(this.baseUrl + `/SupplyPoints/GridSupplyPoints/All`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }
    GetCustomersForGridSupplyPoint(id: number, onLoad: ((numCustomers: number) => void) | undefined) {
        this.http.get<number>(this.baseUrl + `/SupplyPoints/GridSupplyPoint/Customers?id=${id}`).subscribe(result => {
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
        this.http.get<SubstationLoadProfile[]>(this.baseUrl + `/LoadProfiles/GeographicalAreaLoadProfiles?id=${id}&source=${source}&year=${year}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    GetGridSupplyPointLoadProfiles(id: number, source: LoadProfileSource, year: number, onLoad: (loadProfiles: SubstationLoadProfile[]) => void | undefined) {
        this.http.get<SubstationLoadProfile[]>(this.baseUrl + `/LoadProfiles/GridSupplyPointLoadProfiles?id=${id}&source=${source}&year=${year}`).subscribe(result => {
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
     * GIS
     */
    GetGISBoundaries(gisDataId: number, onLoad: (boundaries: GISBoundary[]) => void | undefined) {
        this.http.get<GISBoundary[]>(this.baseUrl + `/GIS/Boundaries?gisDataId=${gisDataId}`).subscribe(result => {
            if (onLoad !== undefined) {
                onLoad(result);
            }
        }, error =>  this.logErrorMessage(error));
    }

    /**
     * Classification tool
     */
    RunClassificationTool(input: ClassificationToolInput, onLoad: (output: ClassificationToolOutput) => void, onError?:(resp:any)=>void, onComplete?:()=>void) {
        this.showMessageService.showMessage("Calculating ...");
        this.http.post<ClassificationToolOutput>(this.baseUrl + '/ClassificationTool/Run', input).subscribe(
            result => {
                if (onLoad !== undefined) {
                    onLoad(result);
                }
            }, 
            error => { this.logErrorMessage(error) }, 
            ()=> { 
                this.showMessageService.clearMessage();
                if ( onComplete ) onComplete() 
            }
        );
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
    GetNetworkData( datasetId: number, onLoad: (networkData: NetworkData)=> void | undefined) {
        this.http.get<NetworkData>(this.baseUrl + `/Loadflow/NetworkData?datasetId=${datasetId}`).subscribe( result => {
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => this.logErrorMessage(error));        
    }

    GetLocationData( datasetId: number, onLoad: (locationData: LocationData)=> void | undefined) {
        this.http.get<LocationData>(this.baseUrl + `/Loadflow/LocationData?datasetId=${datasetId}`).subscribe( result => {
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => this.logErrorMessage(error));        
    }

    GetBoundaries( datasetId: number, onLoad: (boundaryData: DatasetData<Boundary>)=> void | undefined) {
        this.http.get<DatasetData<Boundary>>(this.baseUrl + `/Loadflow/Boundaries?datasetId=${datasetId}`).subscribe( result => {
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => this.logErrorMessage(error));        
    }

    RunBaseLoadflow( datasetId: number,onLoad: (results: LoadflowResults)=> void | undefined) {
        this.showMessageService.showMessage("Calculating ...");
        this.http.post<LoadflowResults>(this.baseUrl + `/Loadflow/RunBaseLoadflow?datasetId=${datasetId}`,{}).subscribe( result => {
            this.showMessageService.clearMessage()
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => { this.showMessageService.clearMessage(); this.logErrorMessage(error)} );        
    }

    SetBound( datasetId: number, boundaryName:string, onLoad: (results: LoadflowResults)=> void | undefined) {
        this.showMessageService.showMessage("Calculating ...");
        this.http.post<LoadflowResults>(this.baseUrl + `/Loadflow/SetBound?datasetId=${datasetId}&boundaryName=${boundaryName}`,{}).subscribe( result => {
            this.showMessageService.clearMessage()
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => { this.showMessageService.clearMessage(); this.logErrorMessage(error)} );        
    }

    RunBoundaryTrip( datasetId: number, boundaryName: string, tripName: string, onLoad: (results: LoadflowResults)=> void | undefined) {
        this.showMessageService.showMessage("Calculating ...");
        this.http.post<LoadflowResults>(this.baseUrl + `/Loadflow/RunBoundaryTrip?datasetId=${datasetId}&boundaryName=${boundaryName}&tripName=${tripName}`,{}).subscribe( result => {
            this.showMessageService.clearMessage()
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => { this.showMessageService.clearMessage(); this.logErrorMessage(error)} );        
    }

    RunAllBoundaryTrips( datasetId: number, boundaryName: string,  onLoad: (results: LoadflowResults)=> void | undefined) {
        let connectionId = this.signalRService.hubConnection?.connectionId;
        this.showMessageService.showMessage("Calculating ...");
        this.http.post<LoadflowResults>(this.baseUrl + `/Loadflow/RunAllBoundaryTrips?datasetId=${datasetId}&boundaryName=${boundaryName}&connectionId=${connectionId}`,{}).subscribe( result => {
            this.showMessageService.clearMessage()
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => { this.showMessageService.clearMessage(); this.logErrorMessage(error)} );        
    }

    /**
     * Datasets
     */
    Datasets(type: DatasetType, onLoad: (results: Dataset[])=> void | undefined) {
        this.getRequest<Dataset[]>(
            `/Datasets/Datasets?type=${type}`,
            onLoad);
    }

    NewDataset(data: NewDataset, onOk: (resp: string)=> void | undefined, onError: (error: any)=>void | undefined) {
        this.postDialogRequest<NewDataset>('/Datasets/New', data, onOk, onError);
    }

    SaveDataset(data: Dataset, onOk: (resp: string)=> void | undefined, onError: (error: any)=>void | undefined) {
        this.postDialogRequest<Dataset>('/Datasets/Save', data, onOk, onError);
    }

    DeleteDataset(id: number, onLoad: (resp: string)=> void | undefined) {
        this.postRequest<number>('/Datasets/Delete', id, onLoad);
    }

    EditItem(editItem: EditItem, onOk: (resp: DatasetData<any>[])=> void, onError: (error: any)=>void) {
        this.postDialogRequest<EditItem>('/Datasets/EditItem', editItem, onOk, onError);
    }

    DeleteItem(editItem: EditItem, onOk: (resp: any)=> void) {
        this.postRequest<EditItem>('/Datasets/DeleteItem', editItem, onOk);
    }

    UnDeleteItem(editItem: EditItem, onOk: (resp: any)=> void) {
        this.postRequest<EditItem>('/Datasets/UnDeleteItem', editItem, onOk);
    }

    GetDatasetResultCount(datasetId: number, onLoad: (results: number)=> void | undefined) {
        this.getRequest<number>(
            `/Datasets/ResultCount?datasetId=${datasetId}`,
            onLoad);
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

    ElsiDatasetInfo(versionId: number, onLoad: (results: ElsiDatasetInfo)=> void | undefined) {
        this.getRequestWithMessage<ElsiDatasetInfo>('Loading ...',
            `/Elsi/DatasetInfo?versionId=${versionId}`,
            onLoad);
    }

    SaveElsiUserEdit(userEdit: UserEdit, onSave: (resp: string)=>void|undefined) {
        this.postRequest('/Elsi/SaveUserEdit',userEdit,onSave);
    }

    DeleteElsiUserEdit(id: number, onOk: (resp: string)=>void|undefined) {
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

    ForgotPassword(logon: Logon, onOk: (resp: string)=> void | undefined, onError: (error: any)=>void | undefined) {
        this.postDialogRequest<Logon>('/Users/ForgotPassword', logon, onOk, onError);
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

    ResetPassword(resetPassword: ResetPassword, onLoad: (resp: string)=> void | undefined, onError: (error: any)=>void | undefined) {
        this.postDialogRequest<ResetPassword>('/Users/ResetPassword', resetPassword, onLoad, onError);
    }

    GetUsers(onLoad: (resp: User[])=> void | undefined) {
        this.getRequest<User[]>('/Users/Users', onLoad);
    }

    /* Admin */
    SystemInfo(onComplete: (resp:SystemInfo)=>void | undefined) {
        this.getBasicRequest('/Admin/SystemInfo', onComplete)
    }

    BackupDb(onComplete: (resp: any)=> void | undefined) {
        this.getBasicRequest('/Admin/BackupDb', onComplete);
    }

    LoadNetworkData(source: LoadNetworkDataSource, onComplete: (resp: any)=> void | undefined) {
        this.getBasicRequest(`/Admin/LoadNetworkData?source=${source}`, onComplete);
    }

    Logs(onComplete: (resp: any)=> void | undefined, onError: (resp:any)=>void) {
        this.getBasicRequest('/Admin/Logs', onComplete);
    }

    Clear(onComplete: (resp: any)=> void) {
        this.getBasicRequest('/Admin/Logs/Delete', onComplete);
    }

    CancelBackgroundTask(taskId: number, onComplete: (resp: any)=> void | undefined) {
        this.getBasicRequest(`/Admin/Cancel?taskId=${taskId}`, onComplete)
    }

    DataModel(onComplete: (resp: DataModel)=>void | undefined) {
        this.getRequest<DataModel>('/Admin/DataModel',onComplete);
    }

    PerformCleanup(onComplete: (resp: any)=>void | undefined) {
        this.getBasicRequest('/Admin/PerformCleanup', onComplete)
    }

    DeleteAllSubstations(gaId:number, message:string,  onComplete: (resp: any)=>void | undefined) {
        this.postRequestWithMessage(message,`/Admin/DeleteAllSubstations?gaId=${gaId}`,null, onComplete);
    }

    MaintenanceMode(state:boolean, onComplete: (resp: any)=>void | undefined) {
        this.postRequest(`/Admin/MaintenanceMode?state=${state}`,null, onComplete);
    }

    /* National grid */
    GetLoadflowGridSubstations( onLoad: (boundaries: GridSubstation[])=> void | undefined) {
        this.http.get<GridSubstation[]>(this.baseUrl + `/NationalGrid/Loadflow/GridSubstations`).subscribe( result => {
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => this.logErrorMessage(error));        
    }

    /* EV Demand tool */
    RunEvDemandDistributionSubstation(id: number) {
        const params = new HttpParams().append('id', id)
        this.postRequestWithParams('/EVDemand/Run/DistributionSubstation',{},params, ()=>{})
    }

    RunEvDemandPrimarySubstation(id: number) {
        const params = new HttpParams().append('id', id)
        this.postRequestWithParams('/EVDemand/Run/PrimarySubstation',{},params,()=>{})
    }

    RunEvDemandGridSupplyPoint(id: number) {
        const params = new HttpParams().append('id', id)
        this.postRequestWithParams('/EVDemand/Run/GridSupplyPoint',{},params,()=>{})
    }

    /* Solar installations */
    GetSolarInstallationsByGridSupplyPoint(gspId: number,year: number,onLoad: (boundaries: SolarInstallation[])=> void) {
        this.http.get<SolarInstallation[]>(this.baseUrl + `/SolarInstallations/SolarInstallationsByGridSupplyPoint?gspId=${gspId}&year=${year}`).subscribe( result => {
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => this.logErrorMessage(error));        
    }

    GetSolarInstallationsByPrimarySubstation(pssId: number,year: number,onLoad: (boundaries: SolarInstallation[])=> void) {
        this.http.get<SolarInstallation[]>(this.baseUrl + `/SolarInstallations/SolarInstallationsByPrimarySubstation?pssId=${pssId}&year=${year}`).subscribe( result => {
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => this.logErrorMessage(error));        
    }

    GetSolarInstallationsByDistributionSubstation(dssId: number,year: number,onLoad: (boundaries: SolarInstallation[])=> void) {
        this.http.get<SolarInstallation[]>(this.baseUrl + `/SolarInstallations/SolarInstallationsByDistributionSubstation?dssId=${dssId}&year=${year}`).subscribe( result => {
            if ( onLoad ) {
                onLoad(result)
            }
        }, error => this.logErrorMessage(error));        
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

    private postRequestWithParams<T>(url: string, data: T,params: HttpParams, onOk: (resp: string)=>void | undefined) {
        this.http.post<string>(this.baseUrl + url, data, {params: params}).subscribe(resp => {
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

    private postDialogRequest<T>(url: string, data: T,onOk: (resp: any)=>void, onError: (error: any)=> void) {
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