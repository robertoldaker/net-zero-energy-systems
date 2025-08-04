import { Component, Input, OnInit } from '@angular/core';

@Component({
    selector: 'app-quick-help-dialog-group',
    templateUrl: './quick-help-dialog-group.component.html',
    styleUrls: ['./quick-help-dialog-group.component.css']
})
export class QuickHelpDialogGroupComponent  {

    constructor() { }

    @Input()
    helpId: string = ""

    @Input()
    helpTitle: string = ""

}
