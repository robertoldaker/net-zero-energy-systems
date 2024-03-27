import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ElsiDatasetDialogComponent } from 'src/app/elsi/elsi-dataset-dialog/elsi-dataset-dialog.component';
import { DialogFooterButtonsEnum } from '../dialog-footer/dialog-footer.component';

@Component({
    selector: 'app-message-dialog',
    templateUrl: './message-dialog.component.html',
    styleUrls: ['./message-dialog.component.css']
})
export class MessageDialogComponent implements OnInit {

    constructor(public dialogRef: MatDialogRef<ElsiDatasetDialogComponent>, 
        @Inject(MAT_DIALOG_DATA) public data:(MessageDialog)) { 
    }

    ngOnInit(): void {
    }

    get iconName():string {
        switch(this.data.icon) {
            case MessageDialogIcon.Error: {
                return 'error'
                break
            }
            case MessageDialogIcon.Info: {
                return 'info'
                break
            }
            case MessageDialogIcon.Warning: {
                return 'warning'
                break
            }
        }
    }

    get iconColor():string {
        switch(this.data.icon) {
            case MessageDialogIcon.Error: {
                return 'darkred'
                break
            }
            case MessageDialogIcon.Info: {
                return 'green'
                break
            }
            case MessageDialogIcon.Warning: {
                return 'orange'
                break
            }
        }
    }

    ok() {
        this.dialogRef.close(true)
    }

}

export enum MessageDialogIcon {Warning,Error,Info}
export interface MessageDialog {
    message: string
    icon: MessageDialogIcon
    buttons: DialogFooterButtonsEnum
}
