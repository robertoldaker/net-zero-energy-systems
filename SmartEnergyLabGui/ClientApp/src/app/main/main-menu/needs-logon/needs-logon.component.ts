import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { DialogFooterButtonsEnum } from 'src/app/dialogs/dialog-footer/dialog-footer.component';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
    selector: 'app-needs-logon',
    templateUrl: './needs-logon.component.html',
    styleUrls: ['./needs-logon.component.css']
})
export class NeedsLogonComponent implements OnInit {

    constructor(public dialogRef: MatDialogRef<NeedsLogonComponent>, private dialogService: DialogService) { }

    ngOnInit(): void {
    }
    logon() {
        this.dialogService.showLogonDialog()
    }

    register() {
        this.dialogService.showRegisterUserDialog()
    }

    DialogFooterButtons: any = DialogFooterButtonsEnum
}
