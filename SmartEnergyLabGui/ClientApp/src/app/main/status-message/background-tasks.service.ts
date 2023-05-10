import { Injectable } from '@angular/core';
import { DataClientService } from 'src/app/data/data-client.service';
import { SignalRService } from '../signal-r-status/signal-r.service';
import { TaskState } from '../../data/app.data'

@Injectable({
    providedIn: 'root'
})
export class BackgroundTasksService {

    constructor(private signalrService:SignalRService, private dataClientService: DataClientService) {
        signalrService.hubConnection.on('BackgroundTaskUpdate', (data) => {
            this.taskState = data;
            console.log(this.taskState)
        });    
    }

    taskState: TaskState | undefined
    cancel() {
        if ( this.taskState ) {
            this.dataClientService.CancelBackgroundTask(this.taskState.taskId, ()=> {

            });
        }
    }

    close() {
        this.taskState = undefined
    }

}
