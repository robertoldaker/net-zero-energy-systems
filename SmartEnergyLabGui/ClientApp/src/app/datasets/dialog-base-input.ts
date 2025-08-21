import { Component, Input, OnInit } from '@angular/core';
import { DatasetsService } from './datasets.service';
import { DialogBase } from '../dialogs/dialog-base';

@Component({
    selector: 'app-dialog-base-input',
    template: `<p>base works!</p>`,
    styles: []

})
export class DialogBaseInput implements OnInit {

    constructor(protected datasetsService: DatasetsService) { }

    ngOnInit(): void {
    }

    @Input()
    name: string = ""

    @Input()
    dialog: DialogBase = new DialogBase()

    @Input()
    helpId: string = ""

    @Input()
    helpTitle: string = ""

    @Input()
    helpDisabled:boolean = false

    protected get _helpDisabled(): boolean {
        return this.helpId ? this.helpDisabled : true
    }

    get error():string {
        return this.dialog?.getError(this.name)
    }
}
