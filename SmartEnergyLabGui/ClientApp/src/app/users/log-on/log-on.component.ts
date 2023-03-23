import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogBase } from 'src/app/dialogs/diaglog-base';
import { UserService } from '../user.service';

@Component({
    selector: 'app-log-on',
    templateUrl: './log-on.component.html',
    styleUrls: ['./log-on.component.css']
})
export class LogOnComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<LogOnComponent>, private service: DataClientService, private userService: UserService) {
        super();
        this.addFormControl('email')
        this.addFormControl('password')
    }

    ngOnInit(): void {
    }

    logon() {
        this.service.Logon(this.form.value,(resp)=>{
            this.userService.checkLogon();
            this.dialogRef.close();
        },(error)=>{
            this.fillErrors(error);
        }
        )
    }

}
