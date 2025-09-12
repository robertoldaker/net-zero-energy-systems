import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { BoundCalcDataService, BoundCalcLink, BoundCalcLocation, BoundCalcMapSearchItem } from '../../boundcalc-data-service.service';
import { BoundCalcMapComponent } from '../boundcalc-map.component';

@Component({
    selector: 'app-boundcalc-map-search',
    templateUrl: './boundcalc-map-search.component.html',
    styleUrls: ['./boundcalc-map-search.component.css']
})
export class BoundCalcMapSearchComponent implements OnInit {

    constructor(private parent: BoundCalcMapComponent, private boundcalcService: BoundCalcDataService) { }

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
    searchOptions: BoundCalcMapSearchItem[] = []
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
                this.searchOptions = this.boundcalcService.searchMapData(searchStr, 50)
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
        let si:BoundCalcMapSearchItem = e.option.value;
        if (si) {
            if( si.loc ) {
                this.boundcalcService.selectLocation(si.loc.id)
                this.showSearch(false);    
            } else if ( si.link ) {
                this.boundcalcService.selectLink(si.link.id)
                this.showSearch(false);    
            }
        }
    }

    display(id: any) {
        // this pointer is not valid so cannot look up value
        return "";
    }

    getDisplayStr(si: BoundCalcMapSearchItem) {
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
