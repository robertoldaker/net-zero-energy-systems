import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { DialogFooterButtonsEnum } from '../dialog-footer/dialog-footer.component';

@Component({
    selector: 'app-message-dialog',
    templateUrl: './message-dialog.component.html',
    styleUrls: ['./message-dialog.component.css']
})
export class MessageDialogComponent implements OnInit {

    constructor( 
        public dialogRef: MatDialogRef<MessageDialogComponent>,
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
export class MessageDialog {
    constructor(msg:string, icon: MessageDialogIcon = MessageDialogIcon.Info, buttons: DialogFooterButtonsEnum=DialogFooterButtonsEnum.Close) {
        this.message = msg
        this.icon = icon;
        this.buttons = buttons
    }
    message: string;
    icon: MessageDialogIcon;
    buttons: DialogFooterButtonsEnum;
}
