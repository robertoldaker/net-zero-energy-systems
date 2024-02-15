import { AfterViewInit, Component, ElementRef, Inject, Input, ViewChild } from '@angular/core';
import { DistributionSubstation, NameValuePair, SubstationSearchResult } from '../../data/app.data';
import { DataClientService } from '../../data/data-client.service';
import { DialogService } from '../../dialogs/dialog.service';
import { MapDataService } from '../map-data.service';
import { MapPowerService } from '../map-power.service';
import { SearchService } from '../search.service';

@Component({
    selector: 'main-header',
    templateUrl: './main-header.component.html',
    styleUrls: ['./main-header.component.css']
})
export class MainHeaderComponent implements AfterViewInit {

    @Input() title: string = ""
    @ViewChild('searchInput') searchInputRef:ElementRef | undefined

    constructor(private dialogService: DialogService, 
        public mapDataService: MapDataService, 
        public mapPowerService: MapPowerService, 
        public searchService: SearchService, 
        private dataClientService: DataClientService,
        @Inject('MODE') public mode: string) {

    }
    ngAfterViewInit(): void {
    }

    showSearch( value: boolean) {
        this.searchService.isShown = value
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

    searchChanged(e: any) {
    }    

    str: string = ""
    get searchStr():string {
        return this.str;
    }

    searchTimeoutId:any
    searchOptions:SubstationSearchResult[] = []
    autoCompleteOpen: boolean = false;

    set searchStr(value: string) {
        this.str = value;
        if ( this.str.length>=2) {
            // Store just incase it changes before making the call
            let searchStr = this.str;
            if ( this.searchTimeoutId!=undefined) {
                clearTimeout(this.searchTimeoutId);
            }
            this.searchTimeoutId=setTimeout(()=>{
                this.dataClientService.Search(searchStr,50, (results)=>{
                    this.searchOptions = results;
                    if ( this.searchOptions.length==0) {
                        this.autoCompleteOpen = false;
                    }
                })
            }, 250)    
        }
    }

    autoCompleteOpened() {
        this.autoCompleteOpen = true;
    }

    autoCompleteClosed() {
        this.autoCompleteOpen = false;
    }

    searchOptionSelected(e:any) {
        let name:string = e.option.value;
        let selectedObj = this.searchOptions.find(m=>m.name == name)
        if ( selectedObj) {
            this.mapPowerService.setSelectedObj(selectedObj)
        }
    }

    getImageSrc(type: string) {
        let src="/assets/images/"
        if ( type=="GridSupplyPoint") {
            src+="grid-supply-point.png"
        } else if ( type=="PrimarySubstation") {
            src+="primary-substation.png"
        } else if ( type=="DistributionSubstation") {
            src+="distribution-substation.png"
        }
        return src
    }


    runClassificationTool() {
        this.dialogService.showClassificationToolDialog();
    }

    showAboutDialog() {
        this.dialogService.showAboutDialog();
    }

}
