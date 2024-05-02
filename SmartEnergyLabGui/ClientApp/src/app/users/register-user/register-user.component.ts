import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogBase } from 'src/app/dialogs/diaglog-base';
import { ShowMessageComponent } from 'src/app/main/show-message/show-message.component';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';

@Component({
    selector: 'app-register-user',
    templateUrl: './register-user.component.html',
    styleUrls: ['./register-user.component.css']
})
export class RegisterUserComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<RegisterUserComponent>, 
        private service: DataClientService, private messageService: ShowMessageService) { 
        super()
        this.addFormControl('email','')
        this.addFormControl('name','')
        this.addFormControl('password','')
        this.addFormControl('confirmPassword','')
    }

    ngOnInit(): void {
    }

    save() {
        let v = this.form.value
        this.service.SaveNewUser(this.form.value,(resp)=>{
                this.dialogRef.close()
                this.messageService.showMessageWithTimeout("Registration successfull! Please logon using the logon button")
            },(error)=>{
                this.fillErrors(error)
            }
        )
    }

}
