import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { ILoadflowLocation, LocationData } from 'src/app/data/app.data';
import { LoadflowMapComponent } from '../loadflow-map.component';

@Component({
    selector: 'app-loadflow-map-search',
    templateUrl: './loadflow-map-search.component.html',
    styleUrls: ['./loadflow-map-search.component.css']
})
export class LoadflowMapSearchComponent implements OnInit {

    constructor(private parent: LoadflowMapComponent, private loadflowService: LoadflowDataService) { }

    ngOnInit(): void {
    }
    
    @ViewChild('searchInput') searchInputRef:ElementRef | undefined

    str: string = ""
    get searchStr(): string {
        return this.str;
    }

    isShown = false;

    showSearch( value: boolean) {
        this.isShown = value
        if (value && this.searchInputRef!=undefined) {
            this.searchInputRef.nativeElement.focus()
            // Not sure why this is required but it does need it
            setTimeout(()=>{ // this will make the execution after the above boolean has changed
                if ( this.searchInputRef!=undefined) {
                    this.searchInputRef.nativeElement.focus();
                }
              },0);
        }
    }

    searchTimeoutId: any
    searchOptions: ILoadflowLocation[] = []
    autoCompleteOpen: boolean = false;

    set searchStr(value: string) {
        this.str = value;
        if (this.str.length >= 2) {
            // Store just incase it changes before making the call
            let searchStr = this.str;
            if (this.searchTimeoutId != undefined) {
                clearTimeout(this.searchTimeoutId);
            }
            this.searchTimeoutId = setTimeout(() => {
                this.searchOptions = this.loadflowService.searchLocations(searchStr, 50)
                if (this.searchOptions.length == 0) {
                    this.autoCompleteOpen = false;
                }
            }, 250)
        }
    }

    autoCompleteOpened() {
        this.autoCompleteOpen = true;
    }

    autoCompleteClosed() {
        this.autoCompleteOpen = false;
    }

    searchOptionSelected(e: any) {
        let id = e.option.value;
        let selectedObj = this.searchOptions.find(m => m.id == id)
        if (selectedObj) {
            this.loadflowService.selectLocation(selectedObj.id)
            this.searchOptions = []
        }
    }

    display(id: any) {
        // this pointer is not valid so cannot look up value
        return "";
    }

    getDisplayStr(loc: ILoadflowLocation) {
        let str = `(${loc.reference}) ${loc.name}`
        return str;
    }

}
