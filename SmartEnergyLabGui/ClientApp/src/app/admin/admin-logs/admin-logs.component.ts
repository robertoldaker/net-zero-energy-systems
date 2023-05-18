import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { DataClientService } from 'src/app/data/data-client.service';

@Component({
    selector: 'app-admin-logs',
    templateUrl: './admin-logs.component.html',
    styleUrls: ['./admin-logs.component.css']
})
export class AdminLogsComponent implements OnInit {

    @ViewChild('logDiv') logDiv: ElementRef | undefined;
    
    constructor(private dataService: DataClientService) { 
        this.Logs=''
        this.refresh();
    }

    refresh() {
        this.dataService.Logs((resp)=>{
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

    ngOnInit(): void {

    }    

    Logs: string

}
