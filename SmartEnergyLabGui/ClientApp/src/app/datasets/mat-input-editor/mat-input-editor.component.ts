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
            this.editValue = this.input.nativeElement.value
            this.input.nativeElement.value = this.value            
        }
    }

    save(e: Event) {
        e.stopPropagation()
        if ( this.input && this.data ) {
            let value = this.editValue
            if ( value===this.value) {
                return;
            }
            let valueDouble = parseFloat(value)
            if ( this.scalingFac && !isNaN(valueDouble)) {
                value = (valueDouble / this.scalingFac).toString()
            }
            //            
            this.datasetsService.saveUserEditWithPrompt(value, this.data, (resp)=>{
                this.onEdited.emit(this.data)
            });
            if ( this.input ) {
                this.input.nativeElement.blur()
            }    
        }
    }

    cancel(e: Event) {
        e.stopPropagation()
        if ( this.input) {
            this.input.nativeElement.blur()
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

    ngOnInit(): void {
    }

    private editValue: string = ''
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
