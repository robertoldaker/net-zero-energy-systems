import { Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';
import { ILogs } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';

@Component({
    selector: 'app-admin-logs',
    templateUrl: './admin-logs.component.html',
    styleUrls: ['./admin-logs.component.css']
})
export class AdminLogsComponent implements OnInit {

    @ViewChild('logDiv') logDiv: ElementRef | undefined;
    
    constructor() { 
        this.Logs=''
        
    }

    @Input()
    logService: ILogs | undefined

    refresh() {
        if ( this.logService ) {
            this.logService.Logs((resp)=>{
                this.Logs = resp.log;
                if ( this.logDiv) {
                    window.setTimeout(()=>{
                        if ( this.logDiv ) {
                            this.logDiv.nativeElement.scrollTop = this.logDiv.nativeElement.scrollHeight;
                        }
                    }, 200)
                }
            })    
        }
    }

    ngOnInit(): void {
        this.refresh();
    }    

    Logs: string

}
