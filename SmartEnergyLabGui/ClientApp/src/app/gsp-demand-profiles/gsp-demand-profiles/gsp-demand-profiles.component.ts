import { AfterViewInit, Component, ElementRef, HostListener, OnInit, QueryList, ViewChildren } from '@angular/core';
import { GspDemandProfilesService } from '../gsp-demand-profiles-service';
import { EChartsOption } from 'echarts';
import { ComponentBase } from 'src/app/utils/component-base';

export enum ChartType {GBTotal,GroupTotal,Gsp}

@Component({
    selector: 'app-gsp-demand-profiles',
    templateUrl: './gsp-demand-profiles.component.html',
    styleUrls: ['./gsp-demand-profiles.component.css']
})
export class GspDemandProfilesComponent extends ComponentBase implements AfterViewInit {

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
        this.addSub(dataService.GspProfilesLoaded.subscribe((profile) => {
            this.redraw(ChartType.Gsp);
        }))
    }

    ngAfterViewInit(): void {
        this.setChartHeight();
    }

    chartHeight = '200px'

    private setChartHeight() {
        if (this.chartDivs) {
            window.setTimeout(() => {
                if ( this.chartDivs ) {
                    let div = this.chartDivs.first.nativeElement
                    let chartHeight = `${(div.clientHeight - 30)}px`
                    this.chartHeight = chartHeight
                    window.setTimeout(() => {
                        this.redraw(ChartType.GBTotal);
                        this.redraw(ChartType.GroupTotal);
                        this.redraw(ChartType.Gsp);
                    }, 0)
                }
            }, 0)
        }
    }

    @HostListener('window:resize', [])
    onResize() {
        this.setChartHeight()
    }


    ChartType = ChartType


    get gbTotalTitle():string {
        return `GB Total Demand (MW) for ${this.dataService.selectedDate?.toDateString()}`
    }

    get groupTotalTitle(): string {
        return `Total Demand (MW) for group ${this.dataService.selectedGroupId}`
    }

    get gspTitle(): string {
        return `Demand (MW) for GSP ${this.dataService.selectedGspId}`
    }

    get selectedDate():string | undefined {
        return this.dataService.selectedDate?.toDateString()
    }

    get selectedGroupStr():string | undefined {
        return this.dataService.selectedGroupId
    }

    get selectedGspStr():string | undefined {
        let gspId = this.dataService.selectedGspId
        let name = this.dataService.selectedLocation?.name
        if ( gspId && name ) {
            return `${gspId} (${name})`
        } else {
            return ''
        }
    }

    public chartOptionsMap: Map<ChartType,any> = new Map()
    private dataMap: Map<ChartType,[string,number][]> = new Map()

    @ViewChildren('chart')
    chartDivs: QueryList<ElementRef> | undefined

    gridTemplateRows: string = 'auto 0.3333fr 0.3333fr 0.3333fr'
    get chartContainerHeight(): string {
        let offset = '72px'
        return `calc(100vh - ${offset})`
    }

    private getDefaultChartOptions(chartType: ChartType):EChartsOption {
        let lineColor = ''
        if ( chartType == ChartType.GBTotal) {
            lineColor = "rgb(115,119,236)"
        } else if ( chartType == ChartType.GroupTotal) {
            lineColor = "rgb(105,142,78)"
        } else if ( chartType == ChartType.Gsp) {
            lineColor = "rgb(230,82,1)"
        }
        let chartOptions: EChartsOption = {
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
            yAxis: { type: 'value' },
            grid: { left: 50, right: 20, bottom: 40, top: 10 },
            series: [
                { name: 'Demand', type: 'line', symbol: 'none', encode: { x: 'timestamp', y: 'Demand' } }
            ],
            color: [lineColor]
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
        } else if ( type == ChartType.Gsp) {
            return this.dataService.gspTotalProfile
        } else {
            return []
        }
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
            chartInstance.setOption(chartOptions)
            chartInstance.resize()
        }
    }

    private chartInstanceMap:Map<ChartType,any> = new Map()
    onChartInit(e: any, type: ChartType) {
        this.chartInstanceMap.set(type, e)
    }

}
