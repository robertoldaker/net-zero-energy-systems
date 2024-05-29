import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Subscription } from 'rxjs';
import { LoadflowDataService } from '../loadflow-data-service.service';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-loadflow-data',
    templateUrl: './loadflow-data.component.html',
    styleUrls: ['./loadflow-data.component.css']
})
export class LoadflowDataComponent extends ComponentBase {

    constructor(private dataService: LoadflowDataService) { 
        super()
        this.showAllTripResults = false;
        this.selected = new FormControl(0);
        this.addSub( dataService.ResultsLoaded.subscribe( (results) => {
            this.showAllTripResults = results.singleTrips!=null || results.doubleTrips!=null
        }))
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results)=>{
            this.showAllTripResults = false;
        }))
    }
    
    get mapButtonImage(): string {
        return this.showMap ? '/assets/images/table.png' : '/assets/images/world.png'
    }

    showAllTripResults: boolean
    selected: FormControl
    showMap:boolean = false;

    toggleMap() {
        this.showMap = !this.showMap;
    }

}
