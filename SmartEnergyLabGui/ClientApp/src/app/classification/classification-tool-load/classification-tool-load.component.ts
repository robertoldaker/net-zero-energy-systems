import { Component, OnDestroy, OnInit } from '@angular/core';
import { EChartsOption } from 'echarts';
import { ClassificationToolOutput } from '../../data/app.data';
import { ClassificationToolService } from '../classification-tool.service';

@Component({
    selector: 'app-classification-tool-load',
    templateUrl: './classification-tool-load.component.html',
    styleUrls: ['./classification-tool-load.component.css']
})

export class ClassificationToolLoadComponent implements OnInit, OnDestroy {

    private subs1: any
    constructor(private service: ClassificationToolService) {
        this.subs1 = service.OutputLoaded.subscribe((output: ClassificationToolOutput) => {
            this.redraw()
        })
    }

    ngOnDestroy(): void {
        this.subs1.unsubscribe()
    }

    ngOnInit(): void {
    }

    redraw() {
        if (this.chartInstance !== undefined) {
            this.fillChartOptions();
            this.chartInstance.setOption(this.chartOptions)
        }
    }

    //
    // eCharts stuff
    //
    // time as string, Sat, sun, weekday
    data: [string, number, number, number][] = [];

    fillChartOptions() {
        this.data.length = 0;
        if (this.service.output != undefined) {
            let loadProfile = this.service.output.loadProfile
            this.data.length = loadProfile.timeOfDay.length
            for (let i = 0; i < loadProfile.timeOfDay.length; i++) {
                let d: [string, number, number, number] =
                    [loadProfile.timeOfDay[i],
                    loadProfile.saturday.load[i] * loadProfile.saturday.peak,
                    loadProfile.sunday.load[i] * loadProfile.sunday.peak,
                    loadProfile.weekday.load[i] * loadProfile.weekday.peak,
                    ]
                this.data[i] = d
            }
        }
    }

    chartOptions: EChartsOption = {
        legend: {
            type: 'plain',
            align: 'left',
            top: 15
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
            source: this.data,
            dimensions: ['timestamp', 'Saturday', 'Sunday', 'Weekday'],
        },
        xAxis: { type: 'category', name: 'Time of day', nameLocation: 'middle', nameGap: 35, nameTextStyle: { fontWeight: 'bold' } },
        yAxis: { type: 'value', name: 'Load (kWh)', nameLocation: 'middle', nameGap: 55, nameTextStyle: { fontWeight: 'bold' } },
        grid: { left: 70, right: 20, bottom: 60, top: 50 },
        series: [
            { name: 'Saturday', type: 'line', symbol: 'none', encode: { x: 'timestamp', y: 'Saturday' } },
            { name: 'Sunday', type: 'line', symbol: 'none', encode: { x: 'timestamp', y: 'Sunday' } },
            { name: 'Weekday', type: 'line', symbol: 'none', encode: { x: 'timestamp', y: 'Weekday' } }
        ]
    };

    chartInstance: any
    onChartInit(e: any) {
        this.chartInstance = e;
    }


}
