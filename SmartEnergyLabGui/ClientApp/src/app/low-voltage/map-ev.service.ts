import { EventEmitter, Injectable } from '@angular/core';
import { VehicleChargingStation } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';
import { MapDataService } from './map-data.service';

@Injectable({
    providedIn: 'root'
})
export class MapEvService {

    constructor(private dataClientService: DataClientService, private mapDataService: MapDataService) {
        this.loadChargingStations()
        this.mapDataService.GeographicalAreaSelected.subscribe( ()=>{
            this.loadChargingStations()
        })
    }

    private loadChargingStations() {
        if ( this.mapDataService.geographicalArea) {
            this.selectedVehicleChargingStation = undefined
            this.dataClientService.GetVehicleChargingStations( this.mapDataService.geographicalArea.id, (vcss)=>{
                this.vehicleChargingStations = vcss
                this.VehicleChargingStationsLoaded.emit(vcss)
            } );
        }
    }

    vehicleChargingStations: VehicleChargingStation[] = []
    selectedVehicleChargingStation: VehicleChargingStation | undefined

    setSelectedVehicleChargingStation(vcs: VehicleChargingStation) {
        this.selectedVehicleChargingStation = vcs
        this.ObjectSelected.emit(vcs)
    }

    VehicleChargingStationsLoaded = new EventEmitter<VehicleChargingStation[]>()
    ObjectSelected = new EventEmitter<VehicleChargingStation>()

}
