import { Component, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild } from '@angular/core';
import { EChartsOption } from 'echarts';
import { NameValuePair, SubstationLoadProfile, Season, LoadProfileSource } from '../../data/app.data';
import { MapPowerService } from '../map-power.service';

export enum ShowLoadProfilesBy {Month, DayOfWeek, Season} 
export enum LoadProfileType {Base, VehicleCharging, HeatPumps}
export enum ShowDataType {Load, Carbon, Cost}

@Component({
    selector: 'app-load-profiles',
    templateUrl: './load-profiles.component.html',
    styleUrls: ['./load-profiles.component.css']
})  
export class LoadProfilesComponent implements OnInit, OnDestroy {

    private subs1: any
    private subs2: any
    constructor(public mapPowerService: MapPowerService) {
        //
        this.subs1 = this.mapPowerService.LoadProfilesLoaded.subscribe(source => {
           this.redrawSource(source);
        })
        //
        this.subs2 = this.mapPowerService.LoadProfileSourceChanged.subscribe( ()=> {
            this.selectorStr = this.selectorDef
        })
        //
        let yearInts:number[] = [2021,2022,2023,2024,2025,2026,2027,2028,2029,2030,2031,2032,2033,2034,2035,2045,2050]
        this.years = []
        yearInts.forEach(y=>{
            this.years.push({ name: y.toString(), value: y});
        });
        this.year = this.mapPowerService.year;
    }

    get chartContainerHeight():string {
        let offset = this.mapPowerService.HasSolarInstallations ? '120px' : '72px'
        return `calc(100vh - ${offset})`
    }


    ngOnDestroy(): void {
        this.subs1.unsubscribe()
        this.subs2.unsubscribe()
    }

    ngOnInit(): void {
    }

    reload() {
        this.mapPowerService.year = this.year;
        this.mapPowerService.reloadLoadProfiles();
    }

    @Input()
    type: LoadProfileType = LoadProfileType.Base

    showData: ShowDataType = ShowDataType.Load

    year:number
    years:NameValuePair[]=[];
    //
    selectorStr:string = "1"
    months:NameValuePair[] = [
        {name: 'january', value: 1 },
        {name: 'feburary', value: 2},
        {name: 'march', value: 3},
        {name: 'april', value: 4},
        {name: 'may', value: 5},
        {name: 'june', value: 6},
        {name: 'july', value: 7},
        {name: 'august', value: 8},
        {name: 'september', value: 9},
        {name: 'october', value: 10},
        {name: 'november', value: 11},
        {name: 'december', value: 12}
    ]
    //
    seasons:NameValuePair[] = [
        {name: 'Winter', value: Season.Winter},
        {name: 'Spring', value: Season.Spring, disabled: true},
        {name: 'Summer', value: Season.Summer, disabled: true},
        {name: 'Autumn', value: Season.Autumn, disabled: true},
    ]

    //
    daysOfWeek:NameValuePair[] = [
        {name: 'Saturday', value: 0 },
        {name: 'Sunday', value: 1},
        {name: 'Weekday', value: 2},
    ]   

    //
    // Show by stuff to support the menu options
    //
    showBy: ShowLoadProfilesBy = ShowLoadProfilesBy.DayOfWeek
    get isShowByMonth():boolean {
        return this.showBy == ShowLoadProfilesBy.Month
    }
    showByMonth() {
        this.showBy = ShowLoadProfilesBy.Month;
        this.selectorStr = this.selectorDef
        this.redraw();
    }

    get isShowByDayOfWeek():boolean {
        return this.showBy == ShowLoadProfilesBy.DayOfWeek
    }
    showByDayOfWeek() {
        this.showBy = ShowLoadProfilesBy.DayOfWeek;
        this.selectorStr = this.selectorDef
        this.redraw();
    }

    get isShowBySeason():boolean {
        return this.showBy == ShowLoadProfilesBy.Season
    }

    showBySeason() {
        this.showBy = ShowLoadProfilesBy.Season;
        this.selectorStr = this.selectorDef
        this.redraw();
    }

    redraw() {
        this.Redraw.emit()
    }

    redrawSource(source: LoadProfileSource) {
        this.Redraw.emit(source);
    }

    get selector():string {
        let value = ""
        if ( this.isShowByMonth || this.isShowBySeason) {
            value = "Day of week"
        } else if ( this.isShowByDayOfWeek ) {
            if ( this.mapPowerService.isActual ) {
                value = "Month"
            } else if ( this.mapPowerService.isPredicted) {
                value = "Season"
            }
        } 
        return value;
    }

    get selectorDef():string {
        let value:string = ""
        if ( this.isShowByMonth || this.isShowBySeason) {
            value = this.daysOfWeek[0].value.toString()
        } else if ( this.isShowByDayOfWeek ) {
            if ( this.mapPowerService.isActual ) {
                value = this.months[0].value.toString()
            } else if ( this.mapPowerService.isPredicted) {
                value = this.seasons[0].value.toString()
            }
        } 
        return value;
    }

    set selectorDef(value: string) {
        this.selectorStr = value;
    }

    get selectorLabel():string {
        let value = ""
        if ( this.isShowByMonth || this.isShowBySeason) {
            value = "Day of week"
        } else if ( this.isShowByDayOfWeek ) {
            if ( this.mapPowerService.isActual ) {
                value = "Month"
            } else if ( this.mapPowerService.isPredicted) {
                value = "Season"
            }
        } 
        return value;
    }

    get selectors():NameValuePair[] {
        let value:NameValuePair[] = [];
        if ( this.isShowByMonth || this.isShowBySeason) {
            value = this.daysOfWeek
        } else if ( this.isShowByDayOfWeek ) {
            if ( this.mapPowerService.isActual ) {
                value = this.months
            } else if ( this.mapPowerService.isPredicted) {
                value = this.seasons
            }
        } 
        return value;
    }

    showLoad() {
        this.showData = ShowDataType.Load
        this.redraw()
    }
    get isShowLoad():boolean {
        return this.showData == ShowDataType.Load
    }
    showCarbon() {
        this.showData = ShowDataType.Carbon
        this.redraw()
    }
    get isShowCarbon():boolean {
        return this.showData == ShowDataType.Carbon
    }
    showCost() {
        this.showData = ShowDataType.Cost
        this.redraw()
    }
    get isShowCost():boolean {
        return this.showData == ShowDataType.Cost
    }

    loadProfileTypes = LoadProfileType

    Redraw = new EventEmitter<LoadProfileSource|undefined>()


}
