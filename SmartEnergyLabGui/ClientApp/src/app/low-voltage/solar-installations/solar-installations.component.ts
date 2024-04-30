import { Component, OnInit } from '@angular/core';
import { MapPowerService } from '../map-power.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { EChartsOption } from 'echarts';
import { SolarInstallation } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';

@Component({
    selector: 'app-solar-installations',
    templateUrl: './solar-installations.component.html',
    styleUrls: ['./solar-installations.component.css']
})
export class SolarInstallationsComponent extends ComponentBase implements OnInit {

    constructor(private mapPowerService: MapPowerService) { 
        super()
        this.minYear = 2011
        this.maxYear = 2024        
        this.addSub(mapPowerService.AllSolarInstallationsLoaded.subscribe((solarInstallations)=>{
            console.log(`solar installations loaded [${solarInstallations.length}]`)
            this.redraw(solarInstallations)
        }))        
    }

    private genDatasource( sis: SolarInstallation[]): { years: string[], data: number[] } {
        let data = []
        let years = []
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
            years.push(year.toString());
            if ( number ) {
                total+=number
                data.push(total) 
            } else {
                data.push(total)
            }
        }
        return { years: years, data: data}
    }

    ngOnInit(): void {
        if ( this.mapPowerService.AllSolarInstallations.length>0) {
            this.redraw(this.mapPowerService.AllSolarInstallations)
        }
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
        this.chartInstance = e;
        if ( this.mapPowerService.AllSolarInstallations.length>0 ) {
            this.redraw(this.mapPowerService.AllSolarInstallations)
        }
    }

    redraw(solarInstallations: SolarInstallation[]) {
        if (this.chartInstance !== undefined) {
            this.chartOptions = this.genEChartOptions(solarInstallations)
            this.chartInstance.setOption(this.chartOptions)
        }
    }

    private genEChartOptions(solarInstallations:SolarInstallation[]):EChartsOption {

        let ds = this.genDatasource(solarInstallations)
        let options:EChartsOption = {
            xAxis: {
                type: 'category',
                name: 'Year',
                nameLocation: 'middle', nameGap: 25, nameTextStyle: { fontWeight: 'bold'},
                data: ds.years
              },
              animation: false,
              yAxis: {
                type: 'value',
                name: 'Installations',
                nameLocation: 'middle', nameGap: 55, nameTextStyle: { fontWeight: 'bold'},
              },
              grid: { left: 70, right: 20, bottom: 40, top: 40 },
              series: [
                {
                  data: ds.data,
                  type: 'bar'
                }
              ]
        } 
        return options;
    }

    chartInstance: any
    chartOptions: EChartsOption = {}
    minYear: number 
    maxYear: number 

}
