import { Component, Input, OnInit } from '@angular/core';
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
    }

    @Input()
    label: string = ""

    @Input()
    dialog: DialogBase = new DialogBase()

    @Input()
    data:any[] = []

    @Input()
    multiple:boolean = false

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

    get error():string {
        return this.dialog?.getError(this.name)
    }

}
