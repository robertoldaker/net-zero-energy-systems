import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogBase } from 'src/app/dialogs/diaglog-base';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';

@Component({
    selector: 'app-change-password',
    templateUrl: './change-password.component.html',
    styleUrls: ['./change-password.component.css']
})
export class ChangePasswordComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<ChangePasswordComponent>, private service: DataClientService, private messageService: ShowMessageService) {
        super();
        this.addFormControl('password','')
        this.addFormControl('newPassword1','')
        this.addFormControl('newPassword2','')
    }

    ngOnInit(): void {
    }

    save() {
        this.service.ChangePassword(this.form.value,()=>{
            this.dialogRef.close();
            this.messageService.showMessageWithTimeout("Password successfully changed")
        }, (errors) => {
            this.fillErrors(errors)
        })
    }


}
