import { Component, HostListener, OnDestroy, OnInit } from '@angular/core';
import { EChartsOption } from 'echarts';
import { DistributionSubstation, PrimarySubstation, SubstationClassification } from '../../data/app.data';
import { MapPowerService } from '../../low-voltage/map-power.service';

@Component({
    selector: 'app-classification-info',
    templateUrl: './classification-info.component.html',
    styleUrls: ['./classification-info.component.css']
})
export class ClassificationInfoComponent implements OnInit, OnDestroy {

    subs2: any;
    constructor(private mapPowerData: MapPowerService ) {
        this.classifications = []
        //
        this.subs2 = this.mapPowerData.ClassificationsLoaded.subscribe( ()=>{
            this.classifications = this.mapPowerData.Classifications
            this.fillChartOptions();
            if (this.chartInstance !== undefined) {
                this.chartInstance.setOption(this.chartOptions)
            }
        })
    }
    ngOnDestroy(): void {
        this.subs2.unsubscribe()
    }

    ngOnInit(): void {
    }

    classifications: SubstationClassification[]

    //
    // eCharts stuff
    //
    // classification num, numberOfEACs
    data: [string,number][] = [];

    fillChartOptions() {
        this.data.length =0 ;
        let month:number = 1
        if (this.classifications) {
            let classificationData = this.classifications;            
            // initialise the this.data array with 0 values using the first entry
            for( let i=1; i<=8; i++) {
                this.data.push([i.toString(), 0])
            }
            // Loop through looking for data with matching month 
            classificationData.forEach(m => {
                if ( m.num>=1 && m.num<=8 ) {
                    let index = m.num-1;
                    this.data[index][1] = m.numberOfEACs;
                }
            })
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
                if ( typeof(value) === 'number') {
                    str = value.toFixed(2)
                } 
                return str;
            }
        },
        animation: false,        
        dataset: {
            source: this.data,
            dimensions: ['classificationNum', 'numberOfEACs'],
        },
        xAxis: { type: 'category', name: 'Elexon classification',  nameLocation: 'middle', nameGap: 35, nameTextStyle: { fontWeight: 'bold'} },
        yAxis: { type: 'value', name: '', nameLocation: 'middle', nameGap: 35, nameTextStyle: { fontWeight: 'bold'} },
        series: [
            { name: 'Number of customers', type: 'bar', encode: {x: 'classificationNum',y: 'numberOfEACs' } }, 
        ]
    };

    chartInstance: any
    onChartInit(e: any) {
        this.chartInstance = e;
    } 
}
