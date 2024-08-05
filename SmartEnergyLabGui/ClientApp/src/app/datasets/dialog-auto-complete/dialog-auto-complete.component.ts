import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { DialogBaseInput } from '../dialog-base-input';
import { DatasetsService } from '../datasets.service';

@Component({
    selector: 'app-dialog-auto-complete',
    templateUrl: './dialog-auto-complete.component.html',
    styleUrls: ['../dialog-base-input.css','./dialog-auto-complete.component.css']
})
export class DialogAutoCompleteComponent extends DialogBaseInput implements OnInit {

    constructor(ds: DatasetsService) {
        super(ds)
    }

    ngOnInit(): void {
        this.initialValue = this.dialog.form.get(this.name)?.value
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
    selectedObj: any

    lastSearchStr = ''
    initialValue: string = ''

    onBlur(e: any) {
        let fc = this.dialog.form.get(this.name);
        let name;
        if (this.selectedObj) {               
            name = this.getDisplayStrFcn(this.selectedObj)
        } else {
            name = this.initialValue;
        }
        if ( fc) {
            fc.setValue(name)
        }
    }

    onKeyup(e:any) {
        let searchStr = e.target.value 
        if ( this.lastSearchStr!=searchStr) {
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
        this.selectedObj = e.option.value;
        if (this.selectedObj) {               
            let name = this.getDisplayStrFcn(this.selectedObj)
            let fc = this.dialog.form.get(this.name);
            if ( fc) {
                fc.setValue(name)
                this.onSelected.emit(this.selectedObj)
            }
        }
    }
}

export interface ISearchResults {
    text: string
    results: any[]
}