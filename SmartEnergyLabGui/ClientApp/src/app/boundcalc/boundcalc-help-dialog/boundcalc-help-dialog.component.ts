import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { DialogFooterButtonsEnum } from '../../dialogs/dialog-footer/dialog-footer.component';

@Component({
    selector: 'app-boundcalc-help-dialog',
    templateUrl: './boundcalc-help-dialog.component.html',
    styleUrls: ['./boundcalc-help-dialog.component.css']
})
export class BoundCalcHelpDialogComponent implements OnInit {

    constructor(public dialogRef: MatDialogRef<BoundCalcHelpDialogComponent>) { }

    ngOnInit(): void {
    }

    DialogFooterButtons: any = DialogFooterButtonsEnum

}
