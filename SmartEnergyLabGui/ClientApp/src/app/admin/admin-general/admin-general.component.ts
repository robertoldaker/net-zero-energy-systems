import { Component, OnInit } from '@angular/core';
import { DataClientService } from 'src/app/data/data-client.service';
import { EvDemandClientService } from 'src/app/data/ev-demand-client.service';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { MessageDialogIcon } from 'src/app/dialogs/message-dialog/message-dialog.component';
import { EvDemandService } from 'src/app/low-voltage/ev-demand.service';
import { MainService } from 'src/app/main/main.service';

@Component({
    selector: 'app-admin-general',
    templateUrl: './admin-general.component.html',
    styleUrls: ['./admin-general.component.css']
})
export class AdminGeneralComponent implements OnInit {

    constructor(private dataClientService: DataClientService, 
        public evDemandService: EvDemandService, 
        private evDemandClientService: EvDemandClientService,
        public mainService:MainService,
        private dialogService: DialogService ) {
        
    }

    startMaintenance() {
        this.dialogService.showMessageDialog({ message: "This will start maintenance mode. Are you sure?", icon: MessageDialogIcon.Warning}, ()=>{
            this.dataClientService.MaintenanceMode(true,()=>{

            });    
        })
    }

    stopMaintenance() {
        this.dialogService.showMessageDialog({ message: "This will stop maintenance mode. Are you sure?", icon: MessageDialogIcon.Warning}, ()=>{
            this.dataClientService.MaintenanceMode(false,()=>{
                
            });
        })
    }

    ngOnInit(): void {
    }

    restartEVDemandTool() {
        this.evDemandClientService.RestartEVDemand();
    }

    message: string = "";

}
