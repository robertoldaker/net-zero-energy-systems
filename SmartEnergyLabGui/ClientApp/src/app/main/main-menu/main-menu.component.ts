import { Component, Inject, OnInit } from '@angular/core';
import { MainService } from '../main.service';
import { UserService } from 'src/app/users/user.service';

@Component({
    selector: 'app-main-menu',
    templateUrl: './main-menu.component.html',
    styleUrls: ['./main-menu.component.css']
})
export class MainMenuComponent implements OnInit {

    constructor(private mainService: MainService, public userService: UserService, @Inject('DATA_URL') private baseUrl: string) { }

    ngOnInit(): void {

    }

    get version() {
        return this.mainService.version;
    }

    openDocument() {
        window.open(`${this.baseUrl}/documents/DataDigitalisationStrategy.pdf`,"_blank")
    }

}
