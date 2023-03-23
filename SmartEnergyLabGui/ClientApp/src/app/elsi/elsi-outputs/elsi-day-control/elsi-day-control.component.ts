import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Subscription } from 'rxjs';
import { ElsiDayResult, ElsiResult } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { ElsiDataService } from '../../elsi-data.service';

@Component({
    selector: 'app-elsi-day-control',
    templateUrl: './elsi-day-control.component.html',
    styleUrls: ['./elsi-day-control.component.css']
})
export class ElsiDayControlComponent extends ComponentBase implements OnInit {

    constructor(private service: ElsiDataService, private dataClientService: DataClientService) {
        super()
        this.results = []
        this.currentDay=null
        this.day = ''
        this.index = -1
        this.addSub( service.ResultsChange.subscribe((results)=>{
            this.results= results;
            if (this.index<0 && this.results.length>0) {
                this.index = 0;
            } else if ( this.index>=this.results.length) {
                this.index = this.results.length>0 ? 0: -1;
            }
            this.loadCurrentDay()
    }))
    }

    ngOnInit(): void {
    }

    public nextDay() {
        if ( this.results.length>0 ) {
            this.index++;
            if ( this.index>=this.results.length) {
                this.index = 0;
            }
            this.loadCurrentDay();
        }
    }
    public prevDay() {
        if ( this.results.length>0 ) {
            this.index--;
            if ( this.index<0) {
                this.index = this.results.length-1;
            }
            this.loadCurrentDay();
        }
    }

    loadCurrentDay() {
        if ( this.index>=0 ) {
            this.dataClientService.ElsiDayResult(this.results[this.index].id, (dayResult)=>{
                this.currentDay = dayResult;
                this.day = this.currentDay.day.toString();
                this.emitValueChanged();
            })        
        } else {
            this.currentDay = null;
            this.day = ''
            this.emitValueChanged();
        }
    }

    results: ElsiResult[]
    currentDay: ElsiDayResult | null         
    day: string

    change(e: any) {
        this.loadCurrentDay();
    }

    private emitValueChanged() {
        this.valueChanged.emit(this.currentDay)
    }

    input(e: any) {
        this.index = e.value
        if ( this.index < this.results.length ) {
            this.day = this.results[this.index].day.toString();
        }
    }

    index: number

    @Output()
    valueChanged: EventEmitter<ElsiDayResult|null> = new EventEmitter<ElsiDayResult|null>


}
