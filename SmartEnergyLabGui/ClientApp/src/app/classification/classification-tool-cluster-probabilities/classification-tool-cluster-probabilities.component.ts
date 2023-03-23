import { Component, OnDestroy, OnInit } from '@angular/core';
import { EChartsOption } from 'echarts';
import { ClassificationToolOutput } from '../../data/app.data';
import { ClassificationToolService } from '../classification-tool.service';

@Component({
    selector: 'app-classification-tool-cluster-probabilities',
    templateUrl: './classification-tool-cluster-probabilities.component.html',
    styleUrls: ['./classification-tool-cluster-probabilities.component.css']
})
export class ClassificationToolClusterProbabilitiesComponent implements OnInit, OnDestroy {

    private subs1: any
    constructor(private service:ClassificationToolService) { 
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
    // cluster num, probabilty
    data: [string,number][] = [];

    fillChartOptions() {
        if (this.service.output!==undefined) {
            let clusterProbabilities = this.service.output.clusterProbabilities
            this.data.length=clusterProbabilities.length
            // initialise the this.data array with 0 values using the first entry
            for( let i=0; i<clusterProbabilities.length; i++) {
                this.data[i]=[(i+1).toString(), clusterProbabilities[i]]
            }
        } 
    }

    chartOptions: EChartsOption = {
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
            dimensions: ['clusterNumber', 'probability'],
        },
        grid: { left: 70, right: 20, bottom: 60, top: 50 },
        xAxis: { type: 'category', name: 'Cluster number', nameLocation: 'middle', nameGap: 35, nameTextStyle: { fontWeight: 'bold' } },
        yAxis: { type: 'value', name: '', nameLocation: 'middle', nameGap: 35, nameTextStyle: { fontWeight: 'bold' } },
        series: [
            { name: 'Probability', type: 'bar', encode: { x: 'clusterNumber', y: 'probability' } },
        ]
    };

    chartInstance: any
    onChartInit(e: any) {
        this.chartInstance = e;
    }

}
