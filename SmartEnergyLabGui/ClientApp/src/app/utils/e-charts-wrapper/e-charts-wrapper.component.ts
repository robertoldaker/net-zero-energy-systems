import { Component, HostListener, Input, OnInit, Output } from '@angular/core';
import { EChartsOption } from 'echarts';
import { EventEmitter } from '@angular/core';

@Component({
    selector: 'app-e-charts-wrapper',
    templateUrl: './e-charts-wrapper.component.html',
    styleUrls: ['./e-charts-wrapper.component.css']
})
export class EChartsWrapperComponent implements OnInit {

    constructor() {
    }

    ngOnInit(): void {
    }

    @Input() options: EChartsOption = {}
    @Input() styles: any = {}
    @Output() chartInit = new EventEmitter<any>()


    hideChart: boolean = false;
    timeoutId: any = null

    @HostListener('window:resize', [])
    onResize() {
        this.hideChart = true;
        if (this.timeoutId !== null) {
            clearTimeout(this.timeoutId);
        }
        let instance = this;
        this.timeoutId = setTimeout(() => {
            instance.resize();
        }, 250);
    }

    resize() {
        this.hideChart = false
        if (this.chartInstance !== undefined) {
            this.chartInstance.resize()
        }
    }

    chartInstance: any
    onChartInitInt(e: any) {
        this.chartInstance = e;
        if ( this.chartInit!==undefined ) {
            this.chartInit.emit(e)
        }
    }

}
