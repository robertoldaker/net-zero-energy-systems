import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { DialogBaseInput } from '../dialog-base-input';
import { DatasetsService } from '../datasets.service';
import { DialogBase } from 'src/app/dialogs/dialog-base';

@Component({
    selector: 'app-dialog-auto-complete',
    templateUrl: './dialog-auto-complete.component.html',
    styleUrls: ['../dialog-base-input.css','./dialog-auto-complete.component.css']
})
export class DialogAutoCompleteComponent extends DialogBaseInput {

    constructor(ds: DatasetsService) {
        super(ds)
    }

    @Input()
    label: string = ""

    @Input()
    placeholder: string = ""

    @Output()
    onSearch:EventEmitter<ISearchResults> = new EventEmitter<ISearchResults>()

    @Output()
    onSelected:EventEmitter<any> = new EventEmitter<any>()

    @Input()
    getDisplayStrFcn: (value:any) => string = this.getDisplayStr
    getDisplayStr( value: any) {
        return value.name
    }

    searchTimeoutId: any
    searchOptions: any[] = []

    lastSearchStr = ''

    onKeyup(e:any) {
        let searchStr = e.target.value 
        if (searchStr.length >= 2 && this.lastSearchStr!=searchStr) {
            // Store just incase it changes before making the call
            if (this.searchTimeoutId != undefined) {
                clearTimeout(this.searchTimeoutId);
            }
            this.searchTimeoutId = setTimeout(() => {
                let e = {text: searchStr, results: []}
                this.onSearch.emit(e)
                this.searchOptions = e.results 
                this.lastSearchStr = searchStr
            }, 250)
        }
    }

    optionSelected(e: any) {
        let selectedObj = e.option.value;
        if (selectedObj) {               
            let name = this.getDisplayStrFcn(selectedObj)
            let fc = this.dialog.form.get(this.name);
            if ( fc) {
                fc.setValue(name)
                this.onSelected.emit(selectedObj)
            }
        }
    }
}

export interface ISearchResults {
    text: string
    results: any[]
}