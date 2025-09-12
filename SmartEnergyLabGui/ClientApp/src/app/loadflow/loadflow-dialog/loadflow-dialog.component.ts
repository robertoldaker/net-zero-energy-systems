import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { Boundary, BoundaryTrip, Branch, AllTripResult, Dataset, DatasetType, NetworkData, SetPointMode, GenerationModel } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { DataClientService } from 'src/app/data/data-client.service';

@Component({
    selector: 'app-loadflow-dialog',
    templateUrl: './loadflow-dialog.component.html',
    styleUrls: ['./loadflow-dialog.component.css']
})

export class LoadflowDialogComponent extends ComponentBase {

    constructor(private dataService: LoadflowDataService) {
        super()
        this.setBoundaries(dataService.networkData);
        this.branches = dataService.networkData.branches.data;
        this.trips = []
        this.boundaryName="Unspecified"
        this.selectedTrip=""
        this.currentTrip="";
        this.percent = 0;
        this.addSub(dataService.NetworkDataLoaded.subscribe((results=>{
            this.setBoundaries(results)
            this.branches = results.branches.data
            //
            if ( Math.abs(this.dataService.totalDemand - this.dataService.totalGeneration) > 1e-6) {
                this.errorMsg = "Demand does not equal generation"
            } else {
                this.errorMsg = ""
            }
        })))
        this.addSub(dataService.ResultsLoaded.subscribe((results)=>{
            if ( results.boundaryTrips ) {
                this.selectedTrip = "";
                this.trips = results.boundaryTrips.trips
            }
            // this enables the adjustCapacities button and should only appear if we have a capacity error and the dataset is not read only
            //?? No need for this as now done when a new dataset is created
            //??this.hasCapacityError = results.branchCapacityError && !this.dataService.dataset?.isReadOnly;
        }))
        this.addSub(dataService.AllTripsProgress.subscribe((data)=>{
            this.currentTrip = data.msg;
            this.percent = data.percent;
        }))
    }

    errorMsg: string = ""

    setBoundaries(networkData: NetworkData) {
        this.boundaries = networkData.boundaries.data
        this.boundaries.sort( (a,b)=>a.code.localeCompare(b.code))
    }

    calc() {
        let bn:string = this.boundaryName;
        if ( bn == "Unspecified") {
            bn = "";
        }
        this.dataService.runBoundCalc(bn,true);
    }

    getLineNames(trip: BoundaryTrip):string {
        let str = trip.lineNames[0];
        if ( trip.lineNames.length>1) {
            str+=' ' + trip.lineNames[1];
        }
        return str;
    }

    tripSelected(e: any) {
        let trip = this.trips.find( m=>m.text==e.value);
    }

    onDatasetSelected(dataset: Dataset) {
        this.dataService.loadDataset(dataset)
    }

    generationModelChanged(e: any) {
        let tm = this.generationModels.find(m=>m.id === e.value)
        if ( tm ) {
            this.dataService.setGenerationModel(tm);
        }
    }

    setPointModeChanged(e: any) {
        this.dataService.setSetPointMode(e.value);
    }

    get numberOfTrips() {
        return this.dataService.trips.size
    }

    get setPointMode() {
        return this.dataService.setPointMode
    }

    get canSelectManual() {
        return this.dataService.loadFlowResults && (!this.dataService.dataset?.isReadOnly) ? true : false
    }

    get generationModel():GenerationModel | null {
        return this.dataService.generationModel
    }

    get totalGeneration():number {
        return this.dataService.totalGeneration
    }

    get totalMaxTEC():number {
        return this.dataService.totalMaxTEC
    }

    get totalDemand(): number {
        return this.dataService.totalDemand
    }

    get needsCalc():boolean {
        return this.dataService.needsCalc
    }

    boundaryTripChanged(e: any) {
        this.dataService.runBoundaryTrip(e.value)
    }

    get boundaryTripResults():(AllTripResult)[] {
        return this.dataService.boundaryTripResults
    }

    get boundaryTripResult():AllTripResult | undefined {
        return this.dataService.boundaryTripResult
    }

    get generationModels():GenerationModel[] {
        if ( this.dataService.networkData ) {
            return this.dataService.networkData.generationModels.data
        } else {
            return []
        }
    }

    clearTrips(e:any) {
        this.dataService.clearTrips()
    }

    onReload(e: any) {
        this.dataService.reload()
    }

    get nodeMarginals():boolean {
        return this.dataService.nodeMarginals
    }

    set nodeMarginals(value: boolean) {
        this.dataService.setNodeMarginals(value);
    }

    currentTrip: string
    percent: number
    selectedTrip: string
    boundaries: Boundary[] = []
    branches: any;
    get boundaryName():string {
        return this.dataService.boundaryName ? this.dataService.boundaryName : "Unspecified"
    }
    set boundaryName(bn: string) {
        let newBn = bn !== 'Unspecified' ? bn : undefined
        this.dataService.setBoundary(newBn)
    }
    trips: BoundaryTrip[]
    datasetTypes = DatasetType
    SetPointMode = SetPointMode
}
