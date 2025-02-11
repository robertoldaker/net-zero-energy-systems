import { AfterViewInit, Component, OnDestroy, OnInit, QueryList, ViewChild, ViewChildren } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Subscription } from 'rxjs';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { MatTabLabel } from '@angular/material/tabs';

@Component({
    selector: 'app-loadflow-data',
    templateUrl: './loadflow-data.component.html',
    styleUrls: ['./loadflow-data.component.css']
})
export class LoadflowDataComponent extends ComponentBase implements AfterViewInit {

    constructor(private dataService: LoadflowDataService) { 
        super()
        this.showAllTripResults = false;
        this.selected = new FormControl(0);
        this.addSub( dataService.ResultsLoaded.subscribe( (results) => {
            this.showAllTripResults = results.singleTrips!=null || results.doubleTrips!=null || results.intactTrips!=null
        }))
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results)=>{
            this.showAllTripResults = false;
        }))
    }
    ngAfterViewInit(): void {
        // dispatch this so that app-div-auto-scroller can detect size change
        // need to do this otherwise the location of the div is calculated incorrectly
        window.setTimeout(()=>{
            window.dispatchEvent(new Event('resize'));
        })
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

    tabChange(e: any) {
        // dispatch this so that app-div-auto-scroller can detect size change
        window.dispatchEvent(new Event('resize'));
    }

}
