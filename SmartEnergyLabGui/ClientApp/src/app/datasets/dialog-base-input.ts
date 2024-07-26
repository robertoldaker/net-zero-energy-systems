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
}
