import { Component, OnInit } from '@angular/core';
import { ShowMessageService } from './show-message.service';

@Component({
    selector: 'app-show-message',
    templateUrl: './show-message.component.html',
    styleUrls: ['./show-message.component.css']
})

export class ShowMessageComponent implements OnInit {

    constructor(public service: ShowMessageService) { 

    }

    ngOnInit(): void {
    }

    close() {
        this.service.clearMessage()
    }

}
