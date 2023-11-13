import { Component, OnInit } from '@angular/core';
import { DataClientService } from '../../data/data-client.service';
import { DialogService } from '../../dialogs/dialog.service';
import { MapPowerService } from '../map-power.service';
import { EvDemandService } from '../ev-demand.service';

@Component({
    selector: 'app-dist-info-window',
    templateUrl: './dist-info-window.component.html',
    styleUrls: ['./dist-info-window.component.css']
})
export class DistInfoWindowComponent implements OnInit {

    constructor(public mapPowerService: MapPowerService, 
        private dialogService: DialogService, 
        private dataClientService: DataClientService,
        public evDemandService: EvDemandService) {            
    }

    ngOnInit(): void {
    }

    edit() {
        this.dialogService.showDistSubstationEditorDialog();
    }

    runClassificationTool() {
        if ( this.mapPowerService.SelectedDistributionSubstation) {
            let id = this.mapPowerService.SelectedDistributionSubstation.id;
            this.dataClientService.RunClassificationToolOnSubstation(id, ()=>{
                this.mapPowerService.predictedDemand();
                this.mapPowerService.reloadLoadProfiles();
            });
        }
    }

    runEVDemandTool() {
        if ( this.evDemandService.status.isReady && this.mapPowerService.SelectedDistributionSubstation) {
            let id = this.mapPowerService.SelectedDistributionSubstation.id;
            console.log(id)
            this.dataClientService.RunEvDemandDistributionSubstation(id)
        }
    }

}
