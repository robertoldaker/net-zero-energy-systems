import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { DialogFooterButtonsEnum } from '../../dialogs/dialog-footer/dialog-footer.component';

@Component({
    selector: 'app-loadflow-help-dialog',
    templateUrl: './loadflow-help-dialog.component.html',
    styleUrls: ['./loadflow-help-dialog.component.css']
})
export class LoadflowHelpDialogComponent implements OnInit {

    constructor(public dialogRef: MatDialogRef<LoadflowHelpDialogComponent>) { }

    ngOnInit(): void {
    }

    DialogFooterButtons: any = DialogFooterButtonsEnum

}
