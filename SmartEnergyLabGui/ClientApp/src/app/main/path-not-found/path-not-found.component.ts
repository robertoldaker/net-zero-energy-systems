import { Component, OnInit } from '@angular/core';
import { MainService } from '../main.service';

@Component({
    selector: 'app-path-not-found',
    templateUrl: './path-not-found.component.html',
    styleUrls: ['./path-not-found.component.css']
})
export class PathNotFoundComponent implements OnInit {

    constructor(public mainService: MainService) {

    }

    ngOnInit(): void {
    }

    get currentPath():string {
        return window.location.pathname
    }

}
