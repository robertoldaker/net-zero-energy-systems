import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';

@Component({
    selector: 'app-reset-password',
    templateUrl: './reset-password.component.html',
    styleUrls: ['./reset-password.component.css']
})
export class ResetPasswordComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<ResetPasswordComponent>, 
        @Inject(MAT_DIALOG_DATA) public data: any,
        private service: DataClientService, 
        private messageService: ShowMessageService) {
        super();
        this.addFormControl('newPassword1','')
        this.addFormControl('newPassword2','')
    }

    ngOnInit(): void {
    }

    save() {
        let v = this.form.value
        v.token = this.data.token
        this.service.ResetPassword(v,()=>{
            this.dialogRef.close();
            this.messageService.showMessageWithTimeout("Password successfully changed")
        }, (errors) => {
            if ( errors[''] ) {
                this.dialogRef.close();
                this.messageService.showModalErrorMessage(errors[''])
            } else {
                this.fillErrors(errors)
            }
        })
        
    }

}
