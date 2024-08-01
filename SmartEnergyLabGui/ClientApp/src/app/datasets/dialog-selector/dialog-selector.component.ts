import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { DialogBaseInput } from '../dialog-base-input';
import { DatasetsService } from '../datasets.service';

@Component({
    selector: 'app-dialog-selector',
    templateUrl: './dialog-selector.component.html',
    styleUrls: ['../dialog-base-input.css','../dialog-base-input.css']
})
export class DialogSelectorComponent extends DialogBaseInput implements OnInit {

    constructor(ds: DatasetsService) { 
        super(ds)
    }

    ngOnInit(): void {
        // if enum is supplied then overwrite data with this
        if ( this.enum ) {
            this.data = []
            for (var prop in this.enum) {
                let id = parseInt(prop)
                if ( !isNaN(id) ) {
                    this.data.push({ id: id, name: this.enum[prop] })
                }
            }    
        }
    }

    @Input()
    label: string = ""

    @Input()
    dialog: DialogBase = new DialogBase()

    @Input()
    data:any[] = []

    @Input()
    enum:any

    @Input()
    multiple:boolean = false

    @Output()
    onSelectionChange = new EventEmitter<any>()

    selectionChange(e: any) {
        let id = e.value
        let obj:any
        if (this.enum) {
            obj = this.enum[id]
        } else {
            obj = this.data.find(m=>this.valueFcn(m)==id)
        }
        this.onSelectionChange.emit(obj)
    }

    @Input()
    valueFcn(data: any):number {
        if ( data.id ) {
            return data.id
        } else {
            return 0;
        }
    }
    
    @Input()
    displayFcn(data: any):string {
        if ( data.name ) {
            return data.name
        } else {
            return "?";
        }
    }
}
