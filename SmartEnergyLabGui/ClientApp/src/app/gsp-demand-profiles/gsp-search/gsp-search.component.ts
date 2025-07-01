import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { DataClientService } from 'src/app/data/data-client.service';
import { GspDemandProfilesService } from '../gsp-demand-profiles-service';
import { GspDemandProfileData } from 'src/app/data/app.data';

@Component({
    selector: 'app-gsp-search',
    templateUrl: './gsp-search.component.html',
    styleUrls: ['./gsp-search.component.css']
})
export class GspSearchComponent implements OnInit {


    constructor( private dataService: GspDemandProfilesService) { }

    ngOnInit(): void {
    }

    @ViewChild('searchInput') searchInputRef: ElementRef | undefined

    str: string = ""
    get searchStr(): string {
        return this.str;
    }

    isShown = false;

    showSearch(value: boolean) {
        this.isShown = value
        if (value && this.searchInputRef != undefined) {
            // Not sure why this is required but it does need it
            setTimeout(() => { // this will make the execution after the above boolean has changed
                if (this.searchInputRef != undefined) {
                    this.searchInputRef.nativeElement.focus();
                }
            }, 0);
        }
    }

    searchTimeoutId: any
    searchOptions: GspDemandProfileData[] = []
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
                this.searchOptions = this.dataService.searchProfiles(searchStr, 50)
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
        let si: GspDemandProfileData = e.option.value;
        if (si) {
            if (si.location) {
                this.dataService.selectLocation(si.location)
                this.showSearch(false);
            }
        }
    }

    display(id: any) {
        // this pointer is not valid so cannot look up value
        return "";
    }

    getDisplayStr(si: GspDemandProfileData) {
        if (si.location) {
            let str = `(${si.location.reference}) ${si.location.name}`
            return str;
        } else {
            return si.gspId
        }
    }

    onKeydown(e: any) {
        if (e.keyCode == 27) {
            this.showSearch(false)
        }
    }

}
