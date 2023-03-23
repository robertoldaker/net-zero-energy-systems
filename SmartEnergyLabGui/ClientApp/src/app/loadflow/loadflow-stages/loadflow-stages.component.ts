import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { StageResult, StageResultEnum } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';

@Component({
    selector: 'app-loadflow-stages',
    templateUrl: './loadflow-stages.component.html',
    styleUrls: ['./loadflow-stages.component.css']
})
export class LoadflowStagesComponent implements OnInit, OnDestroy {

    private subs1:Subscription

    constructor(private dataService: LoadflowDataService) {
        this.displayedColumns = ['name', 'result', 'comment']; 
        this.stageResults = [];
        this.subs1 = dataService.ResultsLoaded.subscribe((results)=>{
            this.stageResults = results.stageResults.results
        })
    }
    ngOnDestroy(): void {

    }

    ngOnInit(): void {

    }

    //StageResultEnum
    stageResults:StageResult[]
    displayedColumns:string[]
    getStageText(result: StageResultEnum) {
        return StageResultEnum[result];
    }

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
