import { Component, ElementRef, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { DataClientService } from 'src/app/data/data-client.service';
import { CellEditorData } from '../../datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DatasetsService } from 'src/app/datasets/datasets.service';

@Component({
    selector: 'app-mat-input-editor',
    templateUrl: './mat-input-editor.component.html',
    styleUrls: ['./mat-input-editor.component.css']
})
export class MatInputEditorComponent implements OnInit {

    constructor(private datasetsService: DatasetsService) {
    }

    onFocus() {
        this.hasFocus = true
    }

    onBlur() {
        this.hasFocus = false;
        if ( this.input ) {
            this.saveValue()
        }
    }

    keyDown(e: any) {
        if ( e.code === "Escape") {
            if ( this.input) {
                this.input.nativeElement.value = this.value
            }
        }
        if (e.code === "Enter") {
            if (this.input) {
                this.saveValue()
                this.input.nativeElement.blur()
            }
        }
    }

    saveValue() {
        if ( this.input && this.data ) {
            let newValue = this.input.nativeElement.value
            if ( newValue===this.value) {
                return;
            }
            let valueDouble = parseFloat(newValue)
            if ( this.scalingFac && !isNaN(valueDouble)) {
                newValue = (valueDouble / this.scalingFac).toString()
            }
            //
            this.datasetsService.saveUserEditWithPrompt(newValue, this.data, (resp)=>{
                this.onEdited.emit(this.data)
            }, (errors)=>{});
        }
    }

    @ViewChild('input')
    input: ElementRef | undefined;

    onChange(e: any) {
    }

    delete(e: Event) {
        e.stopPropagation()
        if ( this.data ) {
            this.datasetsService.removeUserEditWithPrompt(this.data,(resp)=>{
                this.onEdited.emit(this.data)
            });
        }
    }

    get prevValue():string {
        let pv = this.data?.userEdit?.prevValue
        if ( pv) {
            let pvFloat = parseFloat(pv)
            if ( !isNaN(pvFloat)) {
                return pvFloat.toFixed(this.decimalPlaces)
            }  else {
                return pv;
            }
        } else {
            return ""
        }
    }

    ngOnInit(): void {
    }

    hasFocus: boolean = false

    @Input()
    label: string = ""

    @Input()
    data: CellEditorData | undefined

    @Input()
    scalingFac: number | undefined

    @Input()
    decimalPlaces: number | undefined

    get readOnly(): boolean {
        return this.data ? this.data.dataset.parent==null : true
    }

    @Output()
    onEdited: EventEmitter<CellEditorData> = new EventEmitter<CellEditorData>()

    get value():any {
        let value = this.data?.value
        if ( typeof value == "number" ) {
            if ( this.scalingFac ) {
                value=value*this.scalingFac
            }
            if ( this.decimalPlaces!==undefined ) {
                value = value.toFixed(this.decimalPlaces)
            }
        }
        return value
    }

}
