import { Component, OnInit } from '@angular/core';
import { SignalRService } from './signal-r.service';

@Component({
    selector: 'app-signal-r-status',
    templateUrl: './signal-r-status.component.html',
    styleUrls: ['./signal-r-status.component.css']
})
export class SignalRStatusComponent implements OnInit {

    constructor(public service:SignalRService) {

     }

    ngOnInit(): void {
    }

    

}
