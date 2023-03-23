import { Component, OnInit } from '@angular/core';
import { MainService } from '../main.service';

@Component({
    selector: 'app-main-menu',
    templateUrl: './main-menu.component.html',
    styleUrls: ['./main-menu.component.css']
})
export class MainMenuComponent implements OnInit {

    constructor(private mainService: MainService) { }

    ngOnInit(): void {

    }

    get version() {
        return this.mainService.version;
    }

}
