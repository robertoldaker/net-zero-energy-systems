import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { DialogFooterButtonsEnum } from '../../dialogs/dialog-footer/dialog-footer.component';

@Component({
    selector: 'app-about-boundcalc-dialog',
    templateUrl: './about-boundcalc-dialog.component.html',
    styleUrls: ['./about-boundcalc-dialog.component.css']
})
export class AboutBoundCalcDialogComponent implements OnInit {

    constructor(public dialogRef: MatDialogRef<AboutBoundCalcDialogComponent>) { }

    ngOnInit(): void {
    }

    DialogFooterButtons: any = DialogFooterButtonsEnum

}
