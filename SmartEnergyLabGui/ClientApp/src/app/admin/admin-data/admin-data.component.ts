import { Component, Inject } from '@angular/core';
import { DataModel } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { ApplicationGroup } from 'src/app/data/app.data';

@Component({
  selector: 'app-admin-data',
  templateUrl: './admin-data.component.html',
  styleUrls: ['./admin-data.component.css']
})
export class AdminDataComponent extends ComponentBase {
    
    constructor(private dataService: DataClientService, private dialogService: DialogService, @Inject('DATA_URL') private baseUrl: string) {
        super()
        this.inCleanup = false;
        this.refresh();
    }

    model: DataModel | undefined;

    backupDb() {
        this.dataService.BackupDb((result)=>{
        })
    }

    backupDbLocally(appGroup: ApplicationGroup) {        
        window.location.href = `${this.baseUrl}/Admin/BackupDbLocally?appGroup=${appGroup}`;
    }

    performCleanup() {
        this.inCleanup = true;
        this.dataService.PerformCleanup( (result)=>{
            this.inCleanup = false;
        });
    }

    refresh() {
        this.dataService.GetDataModel((dm)=>{
            this.model = dm;
        });
    }

    tabChange(e: any) {
        // dispatch this so that app-div-auto-scroller can detect size change
        window.setTimeout(()=>{window.dispatchEvent(new Event('resize'))},0)
    }

    inCleanup: boolean
    ApplicationGroup = ApplicationGroup;
    ElsiAndBoundCalc = ApplicationGroup.Elsi | ApplicationGroup.BoundCalc;

}
