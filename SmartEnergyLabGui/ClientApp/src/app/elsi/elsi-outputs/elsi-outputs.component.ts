import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { Day, ElsiDayResult } from 'src/app/data/app.data';
import { ElsiDataService } from '../elsi-data.service';
import { ElsiDayControlComponent } from './elsi-day-control/elsi-day-control.component';
import { ElsiRowExpanderComponent, ElsiRowExpanderSize } from './elsi-row-expander/elsi-row-expander.component';

@Component({
    selector: 'app-elsi-outputs',
    templateUrl: './elsi-outputs.component.html',
    styleUrls: ['./elsi-outputs.component.css']
})
export class ElsiOutputsComponent implements OnInit, OnDestroy {

    private subs:Subscription[] 
    
    constructor(public service: ElsiDataService) {
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
}
