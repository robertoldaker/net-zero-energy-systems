import { Injectable } from '@angular/core';
import { EventEmitter } from '@angular/core';
import { DistributionSubstation, GISData, GeographicalArea, GridSupplyPoint, LoadProfileSource, PrimarySubstation, SolarInstallation, SubstationClassification, SubstationLoadProfile, SubstationSearchResult, VehicleChargingStation } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';
import { MapDataService } from './map-data.service';

@Injectable({
    providedIn: 'root'
})
export class MapPowerService {

    constructor( private dataClient: DataClientService, private mapDataService: MapDataService ) {
        this.geographicalArea = undefined

        this.GridSupplyPoints = []
        this.PrimarySubstations = []
        this.DistributionSubstations = []
        this.Classifications = []
        this.DataClientService = dataClient
        this.clearSelectedObjects()
        this.PerCustomerLoadProfiles = false;
        this.loadProfileSource = LoadProfileSource.LV_Spreadsheet;
        //
        this.mapDataService.GeographicalAreaSelected.subscribe( (ga) => {
            this.geographicalArea = ga;
            this.setSelectedGeographicalArea();
        })
        this.year = 2021;
        this.loadAllGridSupplyPoints();
    }

    private geographicalArea: GeographicalArea | undefined
    DataClientService: DataClientService
    GridSupplyPoints: GridSupplyPoint[]
    PrimarySubstations: PrimarySubstation[]
    DistributionSubstations: DistributionSubstation[]
    LoadProfileMap = new Map<LoadProfileSource,SubstationLoadProfile[]>()
    Classifications: SubstationClassification[]
    SelectedDistributionSubstation: DistributionSubstation | undefined
    SelectedGeographicalArea: GeographicalArea | undefined
    SelectedGridSupplyPoint: GridSupplyPoint | undefined
    SelectedPrimarySubstation: PrimarySubstation | undefined
    SelectedVehicleChargingStation: VehicleChargingStation | undefined

    NumberOfCustomers: number | undefined
    NumberOfDistributionSubstations: number | undefined
    NumberOfPrimarySubstations: number | undefined
    NumberOfPrimarySubstationsForGa: number | undefined
    PerCustomerLoadProfiles: boolean
    NumberOfEVs: number | undefined
    NumberOfHPs: number | undefined

    SolarInstallationsMode: boolean = false
    HasSolarInstallations: boolean = false
    SolarInstallationsYear: number = 2024
    LatestSolarInstallationsYear: number = 2024
    SolarInstallations:SolarInstallation[] = []
    AllSolarInstallations:SolarInstallation[] = []

    private loadProfileSources:LoadProfileSource[] = [LoadProfileSource.LV_Spreadsheet,LoadProfileSource.EV_Pred,LoadProfileSource.HP_Pred]
    public year:number;

    setSelectedDistributionSubstation(distSubstation: DistributionSubstation| undefined) {
        this.clearSelectedObjects()
        this.SelectedDistributionSubstation = distSubstation
        this.NumberOfCustomers = undefined
        if ( distSubstation!=undefined ) { 
            if (distSubstation.substationData) {
                this.NumberOfCustomers = distSubstation.substationData.numCustomers
            }
            this.clearlPsLoaded()
            this.loadProfileSources.forEach(source=>{
                this.DataClientService.GetDistributionSubstationLoadProfiles(distSubstation.id, source, this.year, (loadProfiles) => {
                    this.loadProfilesLoaded(loadProfiles, source)
                })    
            });
            this.DataClientService.GetDistributionSubstationClassifications(distSubstation.id, (classifications) => {
                this.classificationLoaded(classifications)
            })
        } 
        //
        this.loadSolarInstallations()
        //
        this.fireObjectSelectedEvent()
    }

    private clearlPsLoaded() {
        this.NumberOfEVs=undefined;
        this.NumberOfHPs=undefined;
    }

    setSelectedVehicleChargingStation(chargingStation: VehicleChargingStation| undefined) {
        this.clearSelectedObjects()
        this.SelectedVehicleChargingStation = chargingStation;
        this.fireObjectSelectedEvent()
    }

    private loadProfilesLoaded(loadProfiles:SubstationLoadProfile[], source: LoadProfileSource ) {
        if ( source==LoadProfileSource.EV_Pred && loadProfiles.length>0) {
            this.NumberOfEVs = loadProfiles[0].deviceCount;
        } else if ( source == LoadProfileSource.HP_Pred && loadProfiles.length>0) {
            this.NumberOfHPs = loadProfiles[0].deviceCount;
        }
        this.LoadProfileMap.set(source,loadProfiles)
        if ( this.PerCustomerLoadProfiles ) {
            // Only calculate when we have classifications loaded
            this.calcPerCustomerLoadProfiles(source)
        }
        this.LoadProfilesLoaded.emit(source)
    }

    private classificationLoaded(classifications:SubstationClassification[]) {
        this.Classifications = classifications;
        this.ClassificationsLoaded.emit(classifications);
    }

    private calcPerCustomerLoadProfiles(source: LoadProfileSource) {
        let loadProfile = this.LoadProfileMap.get(source);
        if ( loadProfile) {
            loadProfile.forEach(lp => {
                if ( this.NumberOfCustomers!=undefined) {
                    for(let i:number=0;i<lp.data.length;i++) {
                        lp.data[i]/=this.NumberOfCustomers
                    }    
                    for(let i:number=0;i<lp.carbon.length;i++) {
                        lp.carbon[i]/=this.NumberOfCustomers
                    }    
                    for(let i:number=0;i<lp.cost.length;i++) {
                        lp.cost[i]/=this.NumberOfCustomers
                    }    
                }
            });    
        }
    }

    setSelectedGridSupplyPoint(gsp: GridSupplyPoint | undefined, onPrimariesLoaded: (()=>void ) | null = null ) {
        this.clearSelectedObjects()
        this.SelectedGridSupplyPoint = gsp
        if ( gsp!=undefined ) {
            this.DataClientService.GetPrimarySubstationsByGridSupplyPointId(gsp.id, (pss) => {
                this.PrimarySubstations = pss;
                this.NumberOfDistributionSubstations = this.getNumberOfDistributionSubstations(pss)
                this.NumberOfPrimarySubstations = pss.length
                this.PrimarySubstationsLoaded.emit(pss)
                if ( onPrimariesLoaded!==null ) {
                    onPrimariesLoaded();
                }
            });
            this.DataClientService.GetCustomersForGridSupplyPoint(gsp.id,(numCustomers)=> {
                this.NumberOfCustomers = numCustomers
                this.clearlPsLoaded();
                this.loadProfileSources.forEach(source=>{
                    this.DataClientService.GetGridSupplyPointLoadProfiles(gsp.id,  source, this.year, (loadProfiles) => {
                        this.loadProfilesLoaded(loadProfiles,source)
                    })    
                })
            })
            this.HasSolarInstallations = gsp.numberOfSolarInstallations > 0
        } else {
            this.PrimarySubstations = []
            this.PrimarySubstationsLoaded.emit([])
            this.HasSolarInstallations = false
        }
        //
        this.loadSolarInstallations()
        //
        this.fireObjectSelectedEvent()
    }

    private loadSolarInstallations() {
        this.SolarInstallations = []
        if ( this.SelectedGridSupplyPoint ) {
            this.DataClientService.GetSolarInstallationsByGridSupplyPoint(this.SelectedGridSupplyPoint.id,this.SolarInstallationsYear,(solarInstallations)=>{
                this.SolarInstallations = solarInstallations
                this.SolarInstallationsLoaded.emit(solarInstallations)
            })    
            this.DataClientService.GetSolarInstallationsByGridSupplyPoint(this.SelectedGridSupplyPoint.id,this.LatestSolarInstallationsYear,(solarInstallations)=>{
                this.AllSolarInstallations = solarInstallations
                this.AllSolarInstallationsLoaded.emit(solarInstallations)
            })    
        } else if ( this.SelectedPrimarySubstation ) {
            this.DataClientService.GetSolarInstallationsByPrimarySubstation(this.SelectedPrimarySubstation.id,this.SolarInstallationsYear,(solarInstallations)=>{
                this.SolarInstallations = solarInstallations
                this.SolarInstallationsLoaded.emit(solarInstallations)
            })    
            this.DataClientService.GetSolarInstallationsByPrimarySubstation(this.SelectedPrimarySubstation.id,this.LatestSolarInstallationsYear,(solarInstallations)=>{
                this.AllSolarInstallations = solarInstallations
                this.AllSolarInstallationsLoaded.emit(solarInstallations)
            })    
        } else if ( this.SelectedDistributionSubstation ) {
            this.DataClientService.GetSolarInstallationsByDistributionSubstation(this.SelectedDistributionSubstation.id,this.SolarInstallationsYear,(solarInstallations)=>{
                this.SolarInstallations = solarInstallations
                this.SolarInstallationsLoaded.emit(this.SolarInstallations)
            })    
            this.DataClientService.GetSolarInstallationsByDistributionSubstation(this.SelectedDistributionSubstation.id,this.LatestSolarInstallationsYear,(solarInstallations)=>{
                this.AllSolarInstallations = solarInstallations
                this.AllSolarInstallationsLoaded.emit(solarInstallations)
            })    
        } else {
            this.SolarInstallationsLoaded.emit(this.SolarInstallations)
        }
    }

    setSolarInstallationYear( year: number) {
        this.SolarInstallationsYear = year
        this.loadSolarInstallations()
    }

    setSelectedPrimarySubstation(pss: PrimarySubstation | undefined, onDistLoaded: (()=>void) | null = null) {
        this.clearSelectedObjects()
        this.SelectedPrimarySubstation = pss
        if ( pss!=undefined ) {
            this.DataClientService.GetDistributionSubstations(pss.id, (dss) => {
                this.DistributionSubstations = dss;
                this.NumberOfDistributionSubstations = dss.length
                this.DistributionSubstationsLoaded.emit(dss)
                if ( onDistLoaded ) {
                    onDistLoaded()
                }
            });
            this.DataClientService.GetCustomersForPrimarySubstation(pss.id,(numCustomers)=> {
                this.NumberOfCustomers = numCustomers
                this.clearlPsLoaded();
                this.loadProfileSources.forEach(source=>{
                    this.DataClientService.GetPrimarySubstationLoadProfiles(pss.id, source, this.year, (loadProfiles) => {
                        this.loadProfilesLoaded(loadProfiles, source)
                    })    
                })
            })
            this.DataClientService.GetPrimarySubstationClassifications(pss.id, true, (classifications) => {
                this.classificationLoaded(classifications)
            })
        } else {
            this.DistributionSubstations = []
            this.DistributionSubstationsLoaded.emit([])
        }
        //
        this.loadSolarInstallations()
        //
        this.fireObjectSelectedEvent()

    }

    public setSelectedGridSupplyPointById(id: number) {
        let gsp = this.GridSupplyPoints.find(m=>m.id==id)
        if ( gsp) {
            this.setSelectedGridSupplyPoint(gsp);
        }            
    }

    public setSelectedPrimarySubstationById(id: number, parentId: number) {
        let gsp = this.GridSupplyPoints.find(m=>m.id==parentId)
        if ( gsp ) {
            if ( gsp.id!=this.SelectedGridSupplyPoint?.id) {
                this.setSelectedGridSupplyPoint(gsp, ()=>{
                    let pss = this.PrimarySubstations.find(m=>m.id==id)
                    if ( pss ) {
                        // Not ideal but we need to wait for the markers to get created
                        window.setTimeout(()=>{
                            this.setSelectedPrimarySubstation(pss);
                        },250)
                    }
                });    
            } else {
                let pss = this.PrimarySubstations.find(m=>m.id==id)
                if ( pss ) {
                    this.setSelectedPrimarySubstation(pss);
                }
            }
        }            
    }

    private _setSelectedDistributionSubstationById(id: number, parentId: number):boolean {
        let pss = this.PrimarySubstations.find(m=>m.id==parentId)
        if ( pss ) {
            // Not ideal but we need to wait for the markers to get created
            this.setSelectedPrimarySubstation(pss, ()=>{
                let dss = this.DistributionSubstations.find(m=>m.id==id)
                window.setTimeout(()=>{
                     this.setSelectedDistributionSubstation(dss);
                },250)
            });
            //
            return true;
        } else {
            return false;
        }
    }

    public setSelectedDistributionSubstationById(id: number, parentId: number) {
        let dss = this.DistributionSubstations.find(m=>m.id==id)
        if ( dss ) {
            this.setSelectedDistributionSubstation(dss)
        } else {
            if ( !this._setSelectedDistributionSubstationById(id,parentId) ) {
                this.DataClientService.GetPrimarySubstation(parentId,(data)=>{
                    let pss=data
                    let gsp = this.GridSupplyPoints.find(m=>m.id==pss.gspId)
                    if ( gsp) {
                        this.setSelectedGridSupplyPoint(gsp, ()=>{
                            this._setSelectedDistributionSubstationById(id,parentId)
                        });    
                    } 
                })
            }
        }
    }

    public setSelectedObj(selectedObj: SubstationSearchResult) {

        if ( selectedObj.type=="GridSupplyPoint") {
            this.setSelectedGridSupplyPointById(selectedObj.id);
        } else if ( selectedObj.type=="PrimarySubstation") {
            this.setSelectedPrimarySubstationById(selectedObj.id, selectedObj.parentId)
        } else if ( selectedObj.type=="DistributionSubstation") {
            this.setSelectedDistributionSubstationById(selectedObj.id, selectedObj.parentId)
        }
    }

    private getNumberOfCustomers(classifications: SubstationClassification[]):number {
        let numOfCustomers:number = 0;
        classifications.forEach(element => {
            numOfCustomers+=element.numberOfEACs    
        });
        return numOfCustomers
    }

    private getNumberOfDistributionSubstations(primarySubstations:PrimarySubstation[]):number {
        let numOfDistributionSubstations:number = 0;
        primarySubstations.forEach(element => {
            numOfDistributionSubstations+=element.numberOfDistributionSubstations
        });
        return numOfDistributionSubstations
    }

    private getNumberOfPrimarySubstations(gridSupplyPoints:GridSupplyPoint[]):number {
        let numOfPrimarySubstations:number = 0;
        gridSupplyPoints.forEach(element => {
            numOfPrimarySubstations+=element.numberOfPrimarySubstations
        });
        return numOfPrimarySubstations
    }

    setSelectedGeographicalArea() {
        this.clearSelectedObjects()
        if ( this.geographicalArea!=undefined) {            
            if ( this.GridSupplyPoints.length==0) {
                this.DataClientService.GetGridSupplyPoints(this.geographicalArea.id, (gsps) => {
                    this.GridSupplyPoints = gsps
                    this.NumberOfPrimarySubstationsForGa = this.getNumberOfPrimarySubstations(gsps)
                    this.GridSupplyPointsLoaded.emit(gsps)
                });    
            }
            this.clearlPsLoaded();
            this.loadProfileSources.forEach(source=>{
                if ( this.geographicalArea!=undefined) {
                    this.DataClientService.GetGeographicalAreaLoadProfiles(this.geographicalArea.id,  source, this.year, (loadProfiles) => {
                        this.loadProfilesLoaded(loadProfiles,source)
                    })    
                }
            })
            this.DataClientService.GetGeographicalAreaClassifications(this.geographicalArea.id, true, (classifications) => {
                this.classificationLoaded(classifications)
            })
        } 
        this.fireObjectSelectedEvent()
    }

    loadAllGridSupplyPoints() {
        this.DataClientService.GetAllGridSupplyPoints((gsps) => {
            this.GridSupplyPoints = gsps
            this.NumberOfPrimarySubstationsForGa = this.getNumberOfPrimarySubstations(gsps)
            this.GridSupplyPointsLoaded.emit(gsps)
        });    
    }

    reloadLoadProfiles() {
        if ( this.SelectedDistributionSubstation!=undefined) {
            this.setSelectedDistributionSubstation(this.SelectedDistributionSubstation);
        } else if ( this.SelectedPrimarySubstation!=undefined) {
            this.setSelectedPrimarySubstation(this.SelectedPrimarySubstation);
        } else if ( this.SelectedGridSupplyPoint!=undefined) {
            this.setSelectedGridSupplyPoint(this.SelectedGridSupplyPoint);
        }
    }

    reloadSelected() {
        if ( this.SelectedDistributionSubstation!=undefined) {
            this.DataClientService.GetDistributionSubstation(this.SelectedDistributionSubstation.id, (dss)=>{
                this.SelectedDistributionSubstation = dss;
            });
        } 
    }

    private clearSelectedObjects() {
        this.SelectedGeographicalArea = undefined
        this.SelectedGridSupplyPoint = undefined
        this.SelectedPrimarySubstation = undefined
        this.SelectedDistributionSubstation = undefined
        this.SelectedVehicleChargingStation = undefined
    }

    private fireObjectSelectedEvent() {
        if ( this.SelectedPrimarySubstation===undefined && 
            this.SelectedGridSupplyPoint===undefined && 
            this.SelectedDistributionSubstation===undefined && 
            this.SelectedVehicleChargingStation===undefined
                ) {
            this.SelectedGeographicalArea = this.geographicalArea
        } else {
            this.SelectedGeographicalArea = undefined
        }
        this.ObjectSelected.emit()
    }


    actualDemand() {
        this.loadProfileSource = LoadProfileSource.LV_Spreadsheet;
        this.LoadProfileSourceChanged.emit()
        this.reloadLoadProfiles();
    }

    get isActual():boolean {
        return this.loadProfileSource == LoadProfileSource.LV_Spreadsheet;
    }

    get isPredicted():boolean {
        return this.loadProfileSource == LoadProfileSource.Tool;
    }

    predictedDemand() {
        this.loadProfileSource = LoadProfileSource.Tool;
        this.LoadProfileSourceChanged.emit()
        this.reloadLoadProfiles();
    }

    getLoadProfile(source: LoadProfileSource):SubstationLoadProfile[]|undefined {
        return this.LoadProfileMap.get(source)
    }

    gspMarkersReady() {
        this.GridSupplyPointsMarkersReady.emit()
    }

    setZoom(zoom: number) {
        this.ZoomChanged.emit(zoom)
    }

    setPanTo(gisData: GISData, zoom: number) {
        this.PanToChanged.emit({ gisData: gisData, zoom: zoom})
    }

    setSolarInstallationsMode( mode: boolean) {
        this.SolarInstallationsMode = mode
        this.SolarInstallationsModeChanged.emit(mode)
    }

    loadProfileSource: LoadProfileSource 

    ObjectSelected = new EventEmitter()
    GridSupplyPointsLoaded = new EventEmitter<GridSupplyPoint[] | undefined>()
    GridSupplyPointsMarkersReady = new EventEmitter()
    PrimarySubstationsLoaded = new EventEmitter<PrimarySubstation[] | undefined>()
    DistributionSubstationsLoaded = new EventEmitter<DistributionSubstation[] | undefined>()
    VehicleChargingStationsLoaded = new EventEmitter<VehicleChargingStation[] | undefined>()
    LoadProfilesLoaded = new EventEmitter<LoadProfileSource>()
    ClassificationsLoaded = new EventEmitter<SubstationClassification[] | undefined>()
    LoadProfileSourceChanged = new EventEmitter()
    SolarInstallationsLoaded = new EventEmitter<SolarInstallation[]>()
    AllSolarInstallationsLoaded = new EventEmitter<SolarInstallation[]>()
    SolarInstallationsModeChanged = new EventEmitter<boolean>();
    ZoomChanged = new EventEmitter<number>()
    PanToChanged = new EventEmitter<{gisData: GISData, zoom: number}>()
}
