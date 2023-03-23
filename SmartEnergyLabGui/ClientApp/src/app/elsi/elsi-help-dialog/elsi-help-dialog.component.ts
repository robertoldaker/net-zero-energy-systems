import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { DialogFooterButtonsEnum } from 'src/app/dialogs/dialog-footer/dialog-footer.component';

@Component({
  selector: 'app-elsi-help-dialog',
  templateUrl: './elsi-help-dialog.component.html',
  styleUrls: ['./elsi-help-dialog.component.css']
})
export class ElsiHelpDialogComponent implements OnInit {

    constructor(public dialogRef: MatDialogRef<ElsiHelpDialogComponent>) { }

    ngOnInit(): void {
    }

    DialogFooterButtons: any = DialogFooterButtonsEnum
}
