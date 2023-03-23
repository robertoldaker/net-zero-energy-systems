import { Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';
import { DataClientService } from 'src/app/data/data-client.service';
import { ElsiDataService } from 'src/app/elsi/elsi-data.service';
import { CellEditorData } from '../cell-editor/cell-editor.component';

@Component({
    selector: 'app-mat-input-editor',
    templateUrl: './mat-input-editor.component.html',
    styleUrls: ['./mat-input-editor.component.css']
})
export class MatInputEditorComponent implements OnInit {

    constructor(private dataService: ElsiDataService) {
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
        if ( this.input ) {
            this.input.nativeElement.value = this.editValue
            let value = this.editValue
            if ( value===this.value) {
                console.log(`Ignoring unchanged value [${value}] [${this.value}]`);
                return;
            }
            let valueDouble = parseFloat(value)
            if ( this.scalingFac && !isNaN(valueDouble)) {
                value = (valueDouble / this.scalingFac).toString()
            }
            let userEdit = this.data.getUserEdit(value);
            this.dataService.addUserEdit(userEdit);
            if ( this.input ) {
                this.input.nativeElement.blur()
            }
    }
    }

    cancel(e: Event) {
        e.stopPropagation()
        console.log('cancel')
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
        if ( this.data.userEdit) {
            this.dataService.removeUserEdit(this.data.userEdit.id);
        }
    }

    ngOnInit(): void {
    }

    private editValue: string = ''
    hasFocus: boolean = false

    @Input()
    label: string = ""

    @Input()
    data: CellEditorData = new CellEditorData()

    @Input()
    scalingFac: number | undefined

    @Input()
    decimalPlaces: number | undefined

    @Input()
    readOnly: boolean | undefined

    get value():any {
        let value = this.data.value
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
