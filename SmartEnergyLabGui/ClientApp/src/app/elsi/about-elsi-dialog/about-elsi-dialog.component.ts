import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { DialogFooterButtonsEnum } from 'src/app/dialogs/dialog-footer/dialog-footer.component';

@Component({
    selector: 'app-about-elsi-dialog',
    templateUrl: './about-elsi-dialog.component.html',
    styleUrls: ['./about-elsi-dialog.component.css']
})
export class AboutElsiDialogComponent implements OnInit {


    constructor(public dialogRef: MatDialogRef<AboutElsiDialogComponent>) { }

    ngOnInit(): void {
    }

    DialogFooterButtons: any = DialogFooterButtonsEnum


}
