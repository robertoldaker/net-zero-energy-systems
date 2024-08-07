import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { StageResult, StageResultEnum } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-loadflow-stages',
    templateUrl: './loadflow-stages.component.html',
    styleUrls: ['./loadflow-stages.component.css']
})
export class LoadflowStagesComponent extends ComponentBase implements AfterViewInit {

    constructor(private dataService: LoadflowDataService) {
        super()
        this.displayedColumns = ['name', 'result', 'comment']; 
        this.stageResults = [];
        this.addSub(dataService.ResultsLoaded.subscribe((results)=>{
            this.stageResults = results.stageResults.results
        }))
        this.addSub(dataService.NetworkDataLoaded.subscribe((results)=>{
            this.stageResults = []
        }))
    }
    ngAfterViewInit(): void {
        if ( this.div ) {
            let element = this.div.nativeElement
            let box = element.getBoundingClientRect()
            element.style.height = `calc(100vh - ${box.top}px)`
        }
    }
    //StageResultEnum
    stageResults:StageResult[]
    displayedColumns:string[]
    getStageText(result: StageResultEnum) {
        return StageResultEnum[result];
    }

    @ViewChild('divContainer')
    div: ElementRef | undefined;

    getIconRef(result: StageResultEnum) {
        if ( result==StageResultEnum.Pass) {
            return 'check_circle'
        } else if ( result==StageResultEnum.Fail) {
            return 'error'
        } else if ( result==StageResultEnum.Warn) {
            return 'warning'
        } else {
            return 'warning'
        }
    }

    getIconColor(result: StageResultEnum) {
        if ( result==StageResultEnum.Pass) {
            return 'green'
        } else if ( result==StageResultEnum.Fail) {
            return 'red'
        } else if ( result==StageResultEnum.Warn) {
            return 'orange'
        } else {
            return 'black'
        }
    }

}
