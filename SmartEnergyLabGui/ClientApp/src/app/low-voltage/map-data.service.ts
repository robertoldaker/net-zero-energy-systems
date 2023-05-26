import { Injectable } from '@angular/core';
import { EventEmitter } from '@angular/core';
import { DistributionSubstation, GeographicalArea, LoadProfileSource, PrimarySubstation, SubstationClassification, SubstationLoadProfile, VehicleChargingStation } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';

@Injectable({
    providedIn: 'root'
})
export class MapDataService {

    constructor( private dataClient: DataClientService ) {
        this.geographicalArea = undefined

        this.DataClientService = dataClient
        //??this.DataClientService.GetGeographicalArea("South West England", (ga) => {
        //??    this.geographicalArea = ga;
        //??    this.selectGeographicalArea()
        //??})
    }

    private _showEVChargers: boolean = false;
    get showEVChargers(): boolean {
        return this._showEVChargers;
    }

    set showEVChargers(value: boolean) {
        this._showEVChargers = value;
    }

    geographicalArea: GeographicalArea | undefined
    DataClientService: DataClientService

    selectGeographicalArea() {
        this.GeographicalAreaSelected?.emit(this.geographicalArea);
    }

    GeographicalAreaSelected:EventEmitter<GeographicalArea> = new EventEmitter<GeographicalArea>()
}