import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DistributionSubstation, NameValuePair, SubstationChargingParams, SubstationClassification, SubstationHeatingParams, SubstationMount, SubstationParams } from '../../data/app.data';
import { DataClientService } from '../../data/data-client.service';
import { DialogService } from '../../dialogs/dialog.service';
import { MapPowerService } from '../map-power.service';
import { UtilsService } from '../../utils/utils.service';

@Component({
    selector: 'app-dist-substation-dialog',
    templateUrl: './dist-substation-dialog.component.html',
    styleUrls: ['./dist-substation-dialog.component.css']
})
export class DistSubstationDialogComponent implements OnInit {

    constructor(
                    private mapPowerService: MapPowerService, 
                    public dialogRef: MatDialogRef<DistSubstationDialogComponent>, 
                    private dialogService: DialogService,
                    private utilsService: UtilsService,
                    private dataClientService: DataClientService,
                    private snackBar: MatSnackBar
        ) { 
        this.heatingParams = { numType1HPs: 0, numType2HPs: 0, numType3HPs: 0 }
        this.chargingParams = { numHomeChargers: 0, numType1EVs: 0, numType2EVs:0, numType3EVs: 0 } 
        if ( mapPowerService.SelectedDistributionSubstation ) {
            let dss:DistributionSubstation = mapPowerService.SelectedDistributionSubstation
            this.params = dss.substationParams
            if ( dss.chargingParams!=null) {
                this.chargingParams = dss.chargingParams
            }
            if ( dss.heatingParams!=null ) {
                this.heatingParams = dss.heatingParams
            }
            this.title = dss.name
        } else {
            this.title = "?"
            this.params = {
                mount: SubstationMount.Ground,
                rating: 0,
                numberOfFeeders: 0,
                percentIndustrialCustomers: 0,
                percentageHalfHourlyLoad: 0,
                totalLength: 0,
                percentageOverhead: 0                
            }
        }
        this.elexonProfile = this.getElexonProfile(mapPowerService.Classifications);
        this.mountKeys = this.utilsService.getNameValuePairs(SubstationMount)
    }

    mountKeys: NameValuePair[] = [];

    ngOnInit(): void {

    }

    private getElexonProfile(cls:SubstationClassification[]):number[] {
        let eP:number[] = []
        eP.length = 8;
        cls.forEach((cl)=>{
            if ( cl.num<8 && cl.num>0) {
                eP[cl.num-1] = cl.numberOfEACs;
            }
        })
        return eP;
    }

    elexonProfile: number[] = [0,0,0,0,0,0,0,0]
    params: SubstationParams
    chargingParams: SubstationChargingParams
    heatingParams: SubstationHeatingParams

    title: string

    save() {
        if ( this.mapPowerService.SelectedDistributionSubstation) {
            let id = this.mapPowerService.SelectedDistributionSubstation.id;
            let name = this.mapPowerService.SelectedDistributionSubstation.name;
            this.dataClientService.SetSubstationParams(id, this.params, ()=>{
                // Perform a reload on the instance stored in mapDataService
                this.mapPowerService.reloadSelected();
                this.snackBar.open(`Successfully saved parameters for [${name}]`,"close")
                this.dialogRef.close();
            });
        }
    }

    cancel() {
        this.dialogRef.close();
    }

}
