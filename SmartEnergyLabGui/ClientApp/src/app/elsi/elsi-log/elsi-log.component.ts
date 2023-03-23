import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { ElsiDataService } from '../elsi-data.service';

@Component({
    selector: 'app-elsi-log',
    templateUrl: './elsi-log.component.html',
    styleUrls: ['./elsi-log.component.css']
})
export class ElsiLogComponent implements OnInit, OnDestroy {
    
    constructor(private service: ElsiDataService) { 
        this.timeoutId = 0;
        this.subs = [];
        this.logMessage = ''
        this.subs.push(service.LogMessageAvailable.subscribe((log)=>{
            this.logMessage += log + '\n';
            if ( this.timeoutId===0) {
                this.timeoutId = window.setTimeout(()=>{
                    if ( this.logDiv ) {
                        this.logDiv.nativeElement.scrollTop = this.logDiv.nativeElement.scrollHeight;
                    }
                    this.timeoutId = 0;
                },250);    
            }
        }));
        this.subs.push(service.RunStart.subscribe(()=>{
            this.logMessage = '';
        }));
    }

    private timeoutId: number
    @ViewChild('log') 
    logDiv: ElementRef | undefined;

    ngOnDestroy(): void {
        this.subs.forEach(s => {
            s.unsubscribe();
        });
    }

    private subs: Subscription[];

    ngOnInit(): void {

    }

    logMessage: string


}
