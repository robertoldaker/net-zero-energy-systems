import { Component, OnInit } from '@angular/core';
import { MapPowerService } from '../map-power.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { EChartsOption } from 'echarts';
import { SolarInstallation } from 'src/app/data/app.data';

@Component({
    selector: 'app-solar-installations',
    templateUrl: './solar-installations.component.html',
    styleUrls: ['./solar-installations.component.css']
})
export class SolarInstallationsComponent extends ComponentBase {

    constructor(private mapPowerService: MapPowerService) { 
        super()
        this.minYear = 2011
        this.maxYear = 2024
        this.addSub(mapPowerService.SolarInstallationsLoaded.subscribe((solarInstallations)=>{
            console.log(`solar installations loaded [${solarInstallations.length}]`)
            this.genDatasource(solarInstallations)
        }))
    }

    private genDatasource( sis: SolarInstallation[]) {
        this.data = []
        this.years = []
        let dataMap = new Map<number,number>()
        for( let i=0;i<sis.length;i++) {
            let si = sis[i]
            let number = dataMap.get(si.year);
            if ( number ) {
                dataMap.set( si.year, number+1);
            } else {
                dataMap.set( si.year,1 );
            }
        }
        //
        let total = 0;
        for ( let year = this.minYear; year<=this.maxYear;year++) {
            let number = dataMap.get(year)
            this.years.push(year.toString());
            if ( number ) {
                total+=number
                this.data.push(total) 
            } else {
                this.data.push(total)
            }
        }
       // if ( this.chartOptions.xAxis) {
       //     this.chartOptions.xAxis.data = 
       // }

    }

    private dataSource: [number,number][] = []
    private years: string[] = []
    private data: number[] = []
    
    chartOptions: EChartsOption = {
        xAxis: {
            type: 'category',
            name: 'Year',
            nameLocation: 'middle', nameGap: 25, nameTextStyle: { fontWeight: 'bold'},
            data: [2011,2012]
          },
          animation: false,
          yAxis: {
            type: 'value',
            name: 'Installations',
            nameLocation: 'middle', nameGap: 55, nameTextStyle: { fontWeight: 'bold'},
          },
          series: [
            {
              data: [100,200],
              type: 'bar'
            }
          ]
    } 
    
    private get yAxis():any {
        return { type: 'value', name: 'Number of installations', nameLocation: 'middle', nameGap: 55, nameTextStyle: { fontWeight: 'bold'} }
    }

    ngOnInit(): void {
    }

    yearChanged(e: any) {
        console.log('year changed')
        console.log(e.value)
    }

    yearInput(e: any) {
        this.mapPowerService.setSolarInstallationYear(e.value)
    }

    get year():number {
        return this.mapPowerService.SolarInstallationsYear
    }

    onChartInit(e: any) {

    }


    minYear: number 
    maxYear: number 

}
