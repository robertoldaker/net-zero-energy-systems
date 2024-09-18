import { Component, Input, OnInit } from '@angular/core';
import { EChartsOption } from 'echarts';
import { LoadProfileSource, SubstationLoadProfile } from '../../data/app.data';
import { LoadProfilesComponent, LoadProfileType, ShowDataType, ShowLoadProfilesBy } from '../load-profiles/load-profiles.component';
import { MapPowerService } from '../map-power.service';

@Component({
    selector: 'app-load-profile',
    templateUrl: './load-profile.component.html',
    styleUrls: ['./load-profile.component.css']
})
export class LoadProfileComponent implements OnInit {

    constructor(public mapPowerService: MapPowerService, private parent: LoadProfilesComponent) {
        //
        this.chartOptions = {};
        parent.Redraw.subscribe((source)=>{
            if ( source == undefined ) {
                this.redraw();
            } else {
                if ( this.type == LoadProfileType.Total ) {
                    if ( this.mapPowerService.LoadProfileMap.size == 3 ) {
                        this.redraw();
                    }
                } else if ( this.getLoadProfileSource() == source) {
                    this.redraw();
                }
            }
        });
    }

    isExpanded = true

    expand( exp: boolean) {
        this.isExpanded = exp;
        this.parent.childExpanded()
    }


    ngOnDestroy(): void {
    }

    ngOnInit(): void {
        this.chartOptions = this.chartOptionsByDayOfWeek
    }

    redraw() {
        this.chartOptions = this.getChartOptions()
        if ( this.chartInstance!=undefined ) {
           this.chartInstance.setOption(this.chartOptions)
           this.chartInstance.resize()
        }
    }

    reload() {
        this.mapPowerService.reloadLoadProfiles();
    }


    @Input()
    type: LoadProfileType = LoadProfileType.Base

    showData: ShowDataType = ShowDataType.Load

    get title():string {
        let title="";
        if ( this.type == LoadProfileType.Base) {
            title='Base Power'
            if ( !this.mapPowerService.PerCustomerLoadProfiles && this.mapPowerService.NumberOfCustomers!==undefined) {
                title+=` (customers: ${this.mapPowerService.NumberOfCustomers})`
            }
        } else if ( this.type == LoadProfileType.VehicleCharging ) {
            title='Vehicle charging'
            if ( !this.mapPowerService.PerCustomerLoadProfiles && this.mapPowerService.NumberOfEVs!==undefined) {
                title+=` (EVs: ${this.mapPowerService.NumberOfEVs?.toFixed(0)})`
            }
        } else if ( this.type == LoadProfileType.HeatPumps ) {
            title='Heat pumps'
            if ( !this.mapPowerService.PerCustomerLoadProfiles && this.mapPowerService.NumberOfHPs!==undefined) {
                title+=` (HPs: ${this.mapPowerService.NumberOfHPs?.toFixed(0)})`
            }
        } else if ( this.type == LoadProfileType.Total ) {
            title='Total'
        } else {
            throw Error(`Unexpected load profile type [${this.type}]`);
        }
        if ( this.mapPowerService.PerCustomerLoadProfiles ) {
            title+=' (per customer)'
        }
        return title
    }

    get imgSrc():string {
        if (this.type == LoadProfileType.Base) {
            return "/assets/images/power-plant.png"
        } else if (this.type == LoadProfileType.VehicleCharging) {
            return "/assets/images/charging-station.png"
        } else if (this.type == LoadProfileType.HeatPumps) {
            return "/assets/images/geothermal-energy.png"
        } else if (this.type == LoadProfileType.Total) {
            return "/assets/images/icons8-sigma-100.png"
        } else {
            throw Error(`Unexpected load profile type [${this.type}]`);
        }
    }

    // time as string, Sat, sun, weekday
    dataByDayOfWeek: [string,number,number,number][] = [];
    // time as string, jan, feb, mar etc.
    dataByMonth: [string,number,number,number,number,number,number,number,number,number,number,number,number][] = [];
    dataBySeason: [string, number, number, number, number][] = []
    
   

    getChartOptions():EChartsOption {
        if ( this.parent.showBy == ShowLoadProfilesBy.Month) {
            this.fillChartOptionsByMonth()
            return this.chartOptionsByMonth;
        } else if ( this.parent.showBy == ShowLoadProfilesBy.DayOfWeek) {
            this.fillChartOptionsByDayOfWeek()
            return this.chartOptionsByDayOfWeek
        } else if ( this.parent.showBy == ShowLoadProfilesBy.Season) {
            this.fillChartOptionsBySeason()
            return this.chartOptionsBySeason
        } else {
            throw new Error(`Unrecognised ShowLoadProfilesBy option ${this.parent.showBy}`)
        }
    }

    fillChartOptionsByDayOfWeek() {
        this.dataByDayOfWeek.length=0;
        let selector:number = parseInt(this.parent.selectorStr);
        let loadProfileData = this.getLoadProfile();            
        if (loadProfileData && loadProfileData.length>0) {
            let mins: number = 0
            // initialise the this.data array with 0 values using the first entry
            loadProfileData[0].data.forEach(m=>{
                if ( loadProfileData ) {
                    let timeStr = this.minsToHHmm(mins)
                    this.dataByDayOfWeek.push([timeStr, 0, 0, 0])
                    mins += loadProfileData[0].intervalMins    
                }
            });
            // Loop through looking for data with matching month 
            loadProfileData.forEach(m => {
                if ( (this.mapPowerService.isActual && m.monthNumber === selector) || (this.mapPowerService.isPredicted && m.season === selector )) {
                    // use the day as an index offset
                    let index:number = m.day + 1
                    let i:number = 0
                    this.getDataArray(m).forEach(value => {
                        this.dataByDayOfWeek[i][index] = value
                        i++;
                    })
                }
            })
        }
        if ( this.chartOptionsByDayOfWeek!=undefined ) {
            this.chartOptionsByDayOfWeek.yAxis = this.yAxis
        }
    }

    private getLoadProfile():SubstationLoadProfile[]|undefined {
        if ( this.type == LoadProfileType.Total) {
            return this.mapPowerService.getTotalLoadProfile();
        } else {
            let source = this.getLoadProfileSource()
            return this.mapPowerService.getLoadProfile(source);    
        }
    }

    private getLoadProfileSource():LoadProfileSource  {
        let source = LoadProfileSource.LV_Spreadsheet
        if ( this.type == LoadProfileType.VehicleCharging) {
            source = LoadProfileSource.EV_Pred
        } else if ( this.type == LoadProfileType.HeatPumps) {
            source = LoadProfileSource.HP_Pred
        }
        return source;
    }

    private getDataArray(lp: SubstationLoadProfile):number[] {
        if ( this.parent.isShowCarbon) {
            return lp.carbon;
        } else if ( this.parent.isShowLoad) {
            return lp.data;
        } else if ( this.parent.isShowCost) {
            return lp.cost;
        } else {
            throw Error(`Unexpected showData value found [${this.showData}]`)            
        }
    }

    fillChartOptionsByMonth() {
        this.dataByMonth.length=0;
        let dayOfWeek:number = parseInt(this.parent.selectorStr);
        let loadProfileData = this.getLoadProfile()
        if (loadProfileData && loadProfileData.length>0) {
            let mins: number = 0
            // initialise the this.data array with 0 values using the first entry
            loadProfileData[0].data.forEach(m=>{
                if ( loadProfileData ) {
                    let timeStr = this.minsToHHmm(mins)
                    this.dataByMonth.push([timeStr, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0])
                    mins += loadProfileData[0].intervalMins    
                }
            });
            // Loop through looking for data with matching month 
            loadProfileData.forEach(m => {
                if ( m.day === dayOfWeek ) {
                    // use the day as an index offset
                    let index:number = m.monthNumber
                    let i:number = 0
                    this.getDataArray(m).forEach(value => {
                        this.dataByMonth[i][index] = value
                        i++;
                    })
                }
            })
        }
        if ( this.chartOptionsByMonth!=undefined ) {
            this.chartOptionsByMonth.yAxis = this.yAxis
        }
    }

    fillChartOptionsBySeason() {
        this.dataBySeason.length=0;
        let dayOfWeek:number = parseInt(this.parent.selectorStr);
        let loadProfileData = this.getLoadProfile()
        if (loadProfileData && loadProfileData.length>0) {
            let mins: number = 0
            // initialise the this.data array with 0 values using the first entry
            loadProfileData[0].data.forEach(m=>{
                if ( loadProfileData) {
                    let timeStr = this.minsToHHmm(mins)
                    this.dataBySeason.push([timeStr, 0, 0, 0, 0])
                    mins += loadProfileData[0].intervalMins    
                }
            });
            // Loop through looking for data with matching month 
            loadProfileData.forEach(m => {
                if ( m.day === dayOfWeek ) {
                    // use the day as an index offset
                    let index:number = m.season + 1
                    let i:number = 0
                    this.getDataArray(m).forEach(value => {
                        this.dataBySeason[i][index] = value
                        i++;
                    })
                }
            })
        }
        //
        if ( this.chartOptionsBySeason!=undefined ) {
            this.chartOptionsBySeason.yAxis = this.yAxis
        }
    }

    private get yAxis():any {
        return { type: 'value', name: this.loadProfileTitle, nameLocation: 'middle', nameGap: 55, nameTextStyle: { fontWeight: 'bold'} }
    }

    minsToHHmm(mins: number): string {
        let hours = Math.floor(mins / 60)
        let ms = (mins - hours * 60)
        let hoursStr = hours.toString()
        let minsStr = ms.toString();
        // Add leading 0
        if (hoursStr.length == 1) hoursStr = "0" + hoursStr;
        if (minsStr.length == 1) minsStr = "0" + minsStr;
        return `${hoursStr}:${minsStr}`
    }

    chartOptions: EChartsOption

    private get loadProfileTitle():string {
        let title="";
        if ( this.type == LoadProfileType.Base ) {
            title = 'Actual '
        } else if ( this.type == LoadProfileType.VehicleCharging || this.type == LoadProfileType.HeatPumps || LoadProfileType.Total) {
            title =  'Predicted '
        } else {
            throw Error("Unexpected path in loadProfileTitle");
        }
        //
        if ( this.parent.isShowLoad ) {
            title += 'load (kw)'
        } else if ( this.parent.isShowCarbon) {
            title += 'carbon (kg/h)'
        } else if ( this.parent.isShowCost) {
            title += 'cost (£/h)'
        } else {
            throw Error(`Unexpected showData value [${this.showData}]`)
        }
        //
        if ( this.type == LoadProfileType.Base) {
            title+=' (2016)'
        }
        //
        return title
    }

    chartOptionsByDayOfWeek: EChartsOption = {
        legend: {
            type: 'plain',
            align: 'left',
            top: 5
        },
        tooltip: {
            trigger: 'axis',
            valueFormatter: (value) => {
                let str = ''
                if ( typeof(value) === 'number') {
                    str = value.toFixed(2)
                } 
                return str;
            }
        },
        animation: false,
        dataset: {
            source: this.dataByDayOfWeek,
            dimensions: ['timestamp', 'Saturday', 'Sunday', 'Weekday'],
        },
        xAxis: { type: 'category', name: 'Time of day',  nameLocation: 'middle', nameGap: 25, nameTextStyle: { fontWeight: 'bold'} },
        yAxis: this.yAxis,
        grid: { left: 70, right: 20, bottom: 40, top: 40 },
        series: [
            { name: 'Saturday', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Saturday' } }, 
            { name: 'Sunday', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Sunday' } },
            { name: 'Weekday', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Weekday'} }        
        ],
    };    

    chartOptionsByMonth: EChartsOption = {
        legend: {
            type: 'plain',
            align: 'left',
            top: 0
        },
        tooltip: {
            trigger: 'axis',
            valueFormatter: (value) => {
                let str = ''
                if ( typeof(value) === 'number') {
                    str = value.toFixed(2)
                } 
                return str;
            }
        },
        animation: false,
        dataset: {
            source: this.dataByMonth,
            dimensions: ['timestamp', 'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
        },
        xAxis: { type: 'category', name: 'Time of day',  nameLocation: 'middle', nameGap: 25, nameTextStyle: { fontWeight: 'bold'} },
        yAxis: this.yAxis,
        grid: { left: 70, right: 20, bottom: 40, top: 40 },
        series: [
            { name: 'Jan', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Jan' } }, 
            { name: 'Feb', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Feb' } },
            { name: 'Mar', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Mar'} },
            { name: 'Apr', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Apr'} },
            { name: 'May', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'May'} },
            { name: 'Jun', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Jun'} },
            { name: 'Jul', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Jul'} },
            { name: 'Aug', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Sug'} },
            { name: 'Sep', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Sep'} },
            { name: 'Oct', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Oct'} },
            { name: 'Nov', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Nov'} },
            { name: 'Dec', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Dec'} },
        ],
        color: ["rgb(115,119,236)", "rgb(53,24,73)", "rgb(105,142,78)", "rgb(113,12,158)", "rgb(35,152,13)", "rgb(116,23,31)", "rgb(46,129,144)", "rgb(12,42,44)", "rgb(230,82,1)", "rgb(226,28,122)", "rgb(118,88,70)", "rgb(199,102,116)"]
    };    

    chartOptionsBySeason: EChartsOption = {
        legend: {
            type: 'plain',
            align: 'left',
            top: 15
        },
        tooltip: {
            trigger: 'axis',
            valueFormatter: (value) => {
                let str = ''
                if ( typeof(value) === 'number') {
                    str = value.toFixed(2)
                } 
                return str;
            }
        },
        animation: false,
        dataset: {
            source: this.dataBySeason,
            dimensions: ['timestamp', 'Winter', 'Spring', 'Summer', 'Autumn'],
        },
        xAxis: { type: 'category', name: 'Time of day',  nameLocation: 'middle', nameGap: 25, nameTextStyle: { fontWeight: 'bold'} },
        yAxis: this.yAxis,
        grid: { left: 70, right: 20, bottom: 40, top: 40 },
        series: [
            { name: 'Winter', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Winter' } }, 
            { name: 'Spring', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Spring' } },
            { name: 'Summer', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Summer'} },
            { name: 'Autumn', type: 'line', symbol: 'none', encode: {x: 'timestamp',y: 'Autumn'} }        
        ],
    };    

    chartInstance: any
    onChartInit(e: any) {
        this.chartInstance = e;
    }
}
