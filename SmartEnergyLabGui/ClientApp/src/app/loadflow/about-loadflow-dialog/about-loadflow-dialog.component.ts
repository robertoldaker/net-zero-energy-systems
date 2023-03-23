import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { DialogFooterButtonsEnum } from '../../dialogs/dialog-footer/dialog-footer.component';

@Component({
    selector: 'app-about-loadflow-dialog',
    templateUrl: './about-loadflow-dialog.component.html',
    styleUrls: ['./about-loadflow-dialog.component.css']
})
export class AboutLoadflowDialogComponent implements OnInit {

    constructor(public dialogRef: MatDialogRef<AboutLoadflowDialogComponent>) { }

    ngOnInit(): void {
    }

    DialogFooterButtons: any = DialogFooterButtonsEnum

}
