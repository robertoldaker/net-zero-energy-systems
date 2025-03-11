import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { StageResult, StageResultEnum } from '../../data/app.data';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { DivAutoScrollerComponent } from 'src/app/utils/div-auto-scroller/div-auto-scroller.component';

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
            if ( this.autoScroller ) {
                this.autoScroller.scrollBottom()
            }
        }))
        this.addSub(dataService.NetworkDataLoaded.subscribe((results)=>{
            this.stageResults = []
        }))
    }
    ngAfterViewInit(): void {
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

    @ViewChild(DivAutoScrollerComponent)
    autoScroller : DivAutoScrollerComponent | undefined;


}
