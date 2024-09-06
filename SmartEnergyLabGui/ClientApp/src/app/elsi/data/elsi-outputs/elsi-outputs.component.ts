import { Component, Inject, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { Day, ElsiDayResult } from 'src/app/data/app.data';
import { ElsiDataService } from '../../elsi-data.service';
import { ElsiDayControlComponent } from './elsi-day-control/elsi-day-control.component';
import { ElsiRowExpanderComponent, ElsiRowExpanderSize } from './elsi-row-expander/elsi-row-expander.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';

@Component({
    selector: 'app-elsi-outputs',
    templateUrl: './elsi-outputs.component.html',
    styleUrls: ['./elsi-outputs.component.css']
})
export class ElsiOutputsComponent implements OnInit, OnDestroy {

    private subs:Subscription[] 
    
    constructor(public service: ElsiDataService, private messageService: ShowMessageService, @Inject('DATA_URL') private baseUrl: string) {
        this.subs = []
        //
        this.dayControl = null
        this.currentDay = null
        this.mismatchExpander = null
        this.availabilityExpander = null
        this.marketPhaseExpander = null
        this.balancePhaseExpander = null
        this.balanceMechanismsExpander = null
    }
    ngOnDestroy(): void {
        this.subs.forEach((s)=>s.unsubscribe());
    }

    ngOnInit(): void {
    }

    dayChanged(d: ElsiDayResult|null) {
        this.currentDay = d;
    }

    getDate(day: number) {
        let zeroDate = new Date(2023, 0); // initialize a date in `year-01-01`
        let date = new Date(zeroDate.setDate(day)); // add the number of days
        const formattedDate = date.toLocaleString("en-GB", {
            day: "numeric",
            month: "short"
          });
        return formattedDate;
    }

    trackFcn(index: number, d: ElsiDayResult):number {
        return d.day;
    }

    private currentDay: ElsiDayResult | null
    public get d(): ElsiDayResult | null {        
        return this.currentDay;
    }

    @ViewChild('dayControl') 
    private dayControl: ElsiDayControlComponent | null;

    @ViewChild('mismatchExpander') 
    private mismatchExpander: ElsiRowExpanderComponent | null;
    isMismatchExpanded() {
        return this.mismatchExpander ? this.mismatchExpander.expanded : true;
    }
    //
    @ViewChild('availabilityExpander') 
    private availabilityExpander: ElsiRowExpanderComponent | null;
    isAvailabilityExpanded() {
        return this.availabilityExpander ? this.availabilityExpander.expanded : true;
    }
    getAvailabilityClass(index: number) {
        return { evenAvailability: (index % 2)===0, oddAvailability: (index % 2)===1};
    }
    //
    @ViewChild('marketPhaseExpander') 
    private marketPhaseExpander: ElsiRowExpanderComponent | null;
    isMarketPhaseExpanded() {
        return this.marketPhaseExpander ? this.marketPhaseExpander.expanded : true;
    }
    getMarketPhaseClass(index: number) {
        return { evenMarketPhase: (index % 2)===0, oddMarketPhase: (index % 2)===1};
    }
    //
    @ViewChild('balancePhaseExpander') 
    private balancePhaseExpander: ElsiRowExpanderComponent | null;
    isBalancePhaseExpanded() {
        return this.balancePhaseExpander ? this.balancePhaseExpander.expanded : true;
    }
    getBalancePhaseClass(index: number) {
        return { evenBalancePhase: (index % 2)===0, oddBalancePhase: (index % 2)===1};
    }
    //
    @ViewChild('balanceMechanismsExpander') 
    private balanceMechanismsExpander: ElsiRowExpanderComponent | null;
    isBalanceMechanismsExpanded() {
        return this.balanceMechanismsExpander ? this.balanceMechanismsExpander.expanded : true;
    }
    getBalanceMechanismClass(index: number) {
        return { evenBalanceMechanism: (index % 2)===0, oddBalanceMechanism: (index % 2)===1};
    }

    public get ElsiRowExpanderSize() {
        return ElsiRowExpanderSize; 
    }

    public downloadAsJson() {
        if ( this.service.dataset) {
            let datasetId = this.service.dataset.id
            let scenario = this.service.scenario
            window.location.href = `${this.baseUrl}/Elsi/DownloadResultsAsJson?datasetId=${datasetId}&scenario=${scenario}`
            this.messageService.showMessageWithTimeout('Download started ...')
        }
    }

    public downloadAsCsv() {
        if ( this.service.dataset) {
            let datasetId = this.service.dataset.id
            let scenario = this.service.scenario
            window.location.href = `${this.baseUrl}/Elsi/DownloadResultsAsCsv?datasetId=${datasetId}&scenario=${scenario}`
            this.messageService.showMessageWithTimeout('Download started ...')
        }        
    }
}
