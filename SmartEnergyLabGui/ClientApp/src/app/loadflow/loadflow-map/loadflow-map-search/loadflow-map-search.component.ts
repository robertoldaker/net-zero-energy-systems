import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { LoadflowDataService, LoadflowLink, LoadflowLocation, LoadflowMapSearchItem } from '../../loadflow-data-service.service';
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
            // Not sure why this is required but it does need it
            setTimeout(()=>{ // this will make the execution after the above boolean has changed
                if ( this.searchInputRef!=undefined) {
                    this.searchInputRef.nativeElement.focus();
                }
              },0);
        }
    }

    searchTimeoutId: any
    searchOptions: LoadflowMapSearchItem[] = []
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
                this.searchOptions = this.loadflowService.searchMapData(searchStr, 50)
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
        let si:LoadflowMapSearchItem = e.option.value;
        if (si) {
            if( si.loc ) {
                this.loadflowService.selectLocation(si.loc.id)
                this.showSearch(false);    
            } else if ( si.link ) {
                this.loadflowService.selectLink(si.link.id)
                this.showSearch(false);    
            }
        }
    }

    display(id: any) {
        // this pointer is not valid so cannot look up value
        return "";
    }

    getDisplayStr(si: LoadflowMapSearchItem) {
        if ( si.loc ) {
            let str = `(${si.loc.reference}) ${si.loc.name}`
            return str;    
        } else if ( si.link ) {
            let str = `${si.link.name}`
            return str
        } else {
            return ''
        }
    }

    onKeydown(e: any) {
        if ( e.keyCode == 27 ) {
            this.showSearch(false)
        }
    }

}
