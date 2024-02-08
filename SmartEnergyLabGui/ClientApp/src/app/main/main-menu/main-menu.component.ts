import { Component, Inject, OnInit } from '@angular/core';
import { MainService } from '../main.service';
import { UserService } from 'src/app/users/user.service';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { MessageDialogIcon } from 'src/app/dialogs/message-dialog/message-dialog.component';

@Component({
    selector: 'app-main-menu',
    templateUrl: './main-menu.component.html',
    styleUrls: ['./main-menu.component.css']
})
export class MainMenuComponent implements OnInit {

    constructor(
        private mainService: MainService, 
        public userService: UserService, 
        private dialogService: DialogService,
        @Inject('DATA_URL') private baseUrl: string) { }

    ngOnInit(): void {

    }

    get version() {
        return this.mainService.version;
    }

    openDocument() {
        if ( !this.userService.user ) {
            this.dialogService.showNeedsLogonDialog()
        } else {
            window.open(`${this.baseUrl}/documents/DataDigitalisationStrategy.pdf`,"_blank")
        }
    }

}
