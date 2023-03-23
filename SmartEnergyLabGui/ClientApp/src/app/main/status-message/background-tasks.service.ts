import { Injectable } from '@angular/core';
import { DataClientService } from 'src/app/data/data-client.service';
import { SignalRService } from '../signal-r-status/signal-r.service';
import { TaskState } from '../../data/app.data'

@Injectable({
    providedIn: 'root'
})
export class BackgroundTasksService {

    constructor(private signalrService:SignalRService, private dataClientService: DataClientService) {
        signalrService.hubConnection.on('BackgroundTaskUpdate_ClassificationTool', (data) => {
            this.taskState = data;
        });    
    }

    taskState: TaskState | undefined
    cancel() {
        this.dataClientService.CancelClassificationTool();
    }

    close() {
        this.taskState = undefined
    }

}
