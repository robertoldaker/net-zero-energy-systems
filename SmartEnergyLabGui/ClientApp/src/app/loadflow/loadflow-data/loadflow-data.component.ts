import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Subscription } from 'rxjs';
import { LoadflowDataService } from '../loadflow-data-service.service';

@Component({
    selector: 'app-loadflow-data',
    templateUrl: './loadflow-data.component.html',
    styleUrls: ['./loadflow-data.component.css']
})
export class LoadflowDataComponent implements OnInit, OnDestroy {

    subs1: Subscription
    constructor(private dataService: LoadflowDataService) { 
        this.showAllTripResults = false;
        this.selected = new FormControl(0);
        this.subs1 = dataService.ResultsLoaded.subscribe( (results) => {
            this.showAllTripResults = results.singleTrips!=null || results.doubleTrips!=null
            this.selected.setValue(3);
        })
    }

    ngOnDestroy(): void {
        this.subs1.unsubscribe();
    }

    ngOnInit(): void {

    }
    
    get mapButtonImage(): string {
        return this.showMap ? '/assets/images/table.png' : '/assets/images/world.png'
    }

    showAllTripResults: boolean
    selected: FormControl
    showMap:boolean = true;

    toggleMap() {
        console.log('toggleMap')
        this.showMap = !this.showMap;
    }

}
