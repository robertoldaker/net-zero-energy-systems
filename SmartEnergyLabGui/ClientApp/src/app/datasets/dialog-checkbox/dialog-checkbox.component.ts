import { Component, Input, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { DialogBaseInput } from '../dialog-base-input';
import { DatasetsService } from '../datasets.service';

@Component({
    selector: 'app-dialog-checkbox',
    templateUrl: './dialog-checkbox.component.html',
    styleUrls: ['../dialog-base-input.css','./dialog-checkbox.component.css']
})
export class DialogCheckboxComponent extends DialogBaseInput implements OnInit {

    constructor(ds: DatasetsService) { 
        super(ds)
    }

    ngOnInit(): void {
    }

    @Input()
    label: string = ""

    @Input()
    dialog: DialogBase = new DialogBase()

}
