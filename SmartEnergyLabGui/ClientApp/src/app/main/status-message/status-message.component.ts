import { Component, OnInit } from '@angular/core';
import { TaskStateEnum } from '../../data/app.data';
import { BackgroundTasksService } from './background-tasks.service';

@Component({
    selector: 'app-status-message',
    templateUrl: './status-message.component.html',
    styleUrls: ['./status-message.component.css']
})

export class StatusMessageComponent implements OnInit {

    constructor(public service: BackgroundTasksService) {

    }

    ngOnInit(): void {
    }

    closeOrCancel() {
        if (this.service.taskState!=undefined) {
            if ( this.service.taskState.state == TaskStateEnum.Running ) {
                this.service.cancel();
            } else {
                this.service.close();
            }
        }
    }

    get buttonText():string {
        let text = ''
        if (this.service.taskState!=undefined) {
            text = this.service.taskState.state == TaskStateEnum.Running ? "Cancel" : "Close"
        }
        return text
    }

}
