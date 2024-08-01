import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { LoadflowNodeDialogComponent } from 'src/app/loadflow/dialogs/loadflow-node-dialog/loadflow-node-dialog.component';
import { DialogBaseInput } from '../dialog-base-input';
import { DatasetsService } from '../datasets.service';

@Component({
    selector: 'app-dialog-text-input',
    templateUrl: './dialog-text-input.component.html',
    styleUrls: ['../dialog-base-input.css','./dialog-text-input.component.css']
})
export class DialogTextInputComponent extends DialogBaseInput implements OnInit {

    constructor(ds: DatasetsService) {
        super(ds)
     }

    ngOnInit(): void {
    }

    @Input()
    label: string = ""

    @Input()
    placeholder: string = ""
    isUserEditReverted = false

    get hasUserEdit(): boolean {
        return this.isUserEditReverted ? false : this.dialog?.getUserEdit(this.name) !== undefined
    }

    revert() {
        this.dialog?.revertToPrevValue(this.name)
        this.isUserEditReverted = true
    }
}
