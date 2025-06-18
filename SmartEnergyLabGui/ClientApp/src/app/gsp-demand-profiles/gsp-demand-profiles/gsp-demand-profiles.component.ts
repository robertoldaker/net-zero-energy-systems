import { Component, OnInit } from '@angular/core';
import { GspDemandProfilesService } from '../gsp-demand-profiles-service';
import { EChartsOption } from 'echarts';
import { ComponentBase } from 'src/app/utils/component-base';

export enum ChartType {GBTotal,GroupTotal,Gsp}

@Component({
    selector: 'app-gsp-demand-profiles',
    templateUrl: './gsp-demand-profiles.component.html',
    styleUrls: ['./gsp-demand-profiles.component.css']
})
export class GspDemandProfilesComponent extends ComponentBase {

    constructor(private dataService: GspDemandProfilesService) {
        super()
        this.dataMap.set(ChartType.GBTotal, [])
        this.dataMap.set(ChartType.GroupTotal, [])
        this.dataMap.set(ChartType.Gsp, [])
        this.chartOptionsMap.set(ChartType.GBTotal,this.getDefaultChartOptions(ChartType.GBTotal))
        this.chartOptionsMap.set(ChartType.GroupTotal, this.getDefaultChartOptions(ChartType.GroupTotal))
        this.chartOptionsMap.set(ChartType.Gsp, this.getDefaultChartOptions(ChartType.Gsp))
        this.addSub(dataService.GBTotalProfileLoaded.subscribe((profile)=>{
            this.redraw(ChartType.GBTotal);
        }))
        this.addSub(dataService.GspGroupTotalProfileLoaded.subscribe((profile) => {
            this.redraw(ChartType.GroupTotal);
        }))
        this.addSub(dataService.GspProfileLoaded.subscribe((profile) => {
            this.redraw(ChartType.Gsp);
        }))
    }

    ChartType = ChartType
    get date():Date | undefined {
        return this.dataService.selectedDate
    }

    set date(value: Date | undefined) {
        this.dataService.selectDate(value)
    }

    get minDate():Date | undefined {
        return this.dataService.dates.length>0 ? this.dataService.dates[0] : undefined
    }

    get maxDate(): Date | undefined {
        return this.dataService.dates.length > 0 ? this.dataService.dates[this.dataService.dates.length-1] : undefined
    }


    public chartOptionsMap: Map<ChartType,any> = new Map()
    private dataMap: Map<ChartType,[string,number][]> = new Map()

    private getDefaultChartOptions(chartType: ChartType):EChartsOption {
        let chartOptions: EChartsOption = {
            legend: {
                type: 'plain',
                align: 'left',
                top: 0
            },
            tooltip: {
                trigger: 'axis',
                valueFormatter: (value) => {
                    let str = ''
                    if (typeof (value) === 'number') {
                        str = value.toFixed(2)
                    }
                    return str;
                }
            },
            animation: false,
            dataset: {
                source: this.dataMap.get(chartType),
                dimensions: ['timestamp', 'Demand'],
            },
            xAxis: { type: 'category', name: 'Time of day', nameLocation: 'middle', nameGap: 25, nameTextStyle: { fontWeight: 'bold' } },
            yAxis: this.getYAxis(chartType),
            grid: { left: 70, right: 20, bottom: 40, top: 40 },
            series: [
                { name: 'Demand', type: 'line', symbol: 'none', encode: { x: 'timestamp', y: 'Demand' } }
            ],
            color: ["rgb(115,119,236)", "rgb(53,24,73)", "rgb(105,142,78)", "rgb(113,12,158)", "rgb(35,152,13)", "rgb(116,23,31)", "rgb(46,129,144)", "rgb(12,42,44)", "rgb(230,82,1)", "rgb(226,28,122)", "rgb(118,88,70)", "rgb(199,102,116)"]
        };
        return chartOptions
    }

    fillChartData( type: ChartType) {
        let targetData = this.dataMap.get(type)
        if ( !targetData ) {
            return
        }
        targetData.length = 0;
        // initialise the this.data array with 0 values using the first entry
        let data = this.getDataSource(type)

        let mins: number = 0
        data.forEach(m => {
                let timeStr = this.minsToHHmm(mins)
                if ( targetData ) {
                    targetData.push([timeStr, 0])
                }
                mins += 30
        });
        // Loop through looking for data with matching month
        // use the day as an index offset
        let i: number = 0
        data.forEach(value => {
            if ( targetData ) {
                targetData[i][1] = value
                i++;
            }
        })
    }

    private getDataSource(type: ChartType):number[] {
        if ( type == ChartType.GBTotal) {
            return this.dataService.gbTotalProfile
        } else if ( type == ChartType.GroupTotal) {
            return this.dataService.groupTotalProfile
        } else if ( type == ChartType.Gsp && this.dataService.selectedProfile) {
            return this.dataService.selectedProfile.demand
        } else {
            return []
        }
    }

    private getYAxis(chartType: ChartType): any {
        let name = ''
        if ( chartType == ChartType.GBTotal) {
            name = "Total GB Demand (MW)"
        } else if ( chartType == ChartType.GroupTotal) {
            name = "GSP group total (MW)"
        } else if ( chartType == ChartType.Gsp) {
            name = "GSP demand (MW)"
        }
        return { type: 'value', name: name, nameLocation: 'middle', nameGap: 55, nameTextStyle: { fontWeight: 'bold' } }
    }

    private minsToHHmm(mins: number): string {
        let hours = Math.floor(mins / 60)
        let ms = (mins - hours * 60)
        let hoursStr = hours.toString()
        let minsStr = ms.toString();
        // Add leading 0
        if (hoursStr.length == 1) hoursStr = "0" + hoursStr;
        if (minsStr.length == 1) minsStr = "0" + minsStr;
        return `${hoursStr}:${minsStr}`
    }

    redraw(type: ChartType) {
        let chartOptions = this.chartOptionsMap.get(type)
        this.fillChartData(type)
        let chartInstance = this.chartInstanceMap.get(type)
        if (chartInstance) {
            console.log('redraw chart instance')
            chartInstance.setOption(chartOptions)
            chartInstance.resize()
        }
    }

    private chartInstanceMap:Map<ChartType,any> = new Map()
    onChartInit(e: any, type: ChartType) {
        this.chartInstanceMap.set(type, e)
    }

}
