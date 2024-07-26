import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { UserService } from '../user.service';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { MessageDialogIcon } from 'src/app/dialogs/message-dialog/message-dialog.component';
import { DialogFooterButtonsEnum } from 'src/app/dialogs/dialog-footer/dialog-footer.component';

@Component({
    selector: 'app-log-on',
    templateUrl: './log-on.component.html',
    styleUrls: ['./log-on.component.css']
})
export class LogOnComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<LogOnComponent>, private service: DataClientService, private userService: UserService, private dialogService: DialogService) {
        super();
        this.addFormControl('email','')
        this.addFormControl('password','')
    }

    ngOnInit(): void {
    }

    logon() {
        let v = this.getFormValues()
        this.service.Logon(v,(resp)=>{
            this.userService.checkLogon();
            this.dialogRef.close();
        },(error)=>{
            this.fillErrors(error);
        }
        )
    }

    forgotPassword() {
        let v = this.getFormValues()
        this.service.ForgotPassword(v,(resp)=>{
            this.dialogRef.close();                    
            this.dialogService.showMessageDialog({
                    message: `An email has been sent to <b>${v.email}</b> with a link to change your password.<br/><br/>PLEASE REMEBER TO CHECK YOUR SPAM FOLDER</br>`,
                    icon: MessageDialogIcon.Info,
                    buttons: DialogFooterButtonsEnum.Close
                }, ()=>{})
        },(error)=>{
            this.fillErrors(error);
        }
        )
    }

    getFormValues() {
        return this.form.value;
    }
}
