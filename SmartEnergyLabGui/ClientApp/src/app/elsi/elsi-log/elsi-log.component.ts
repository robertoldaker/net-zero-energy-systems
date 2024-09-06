import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { ElsiDataService } from '../elsi-data.service';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-elsi-log',
    templateUrl: './elsi-log.component.html',
    styleUrls: ['./elsi-log.component.css']
})
export class ElsiLogComponent extends ComponentBase implements AfterViewInit {
    
    constructor(private service: ElsiDataService) { 
        super()
        this.timeoutId = 0;
        this.logMessage = ''
        this.addSub(service.LogMessageAvailable.subscribe((log)=>{
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
        this.addSub(service.RunStart.subscribe(()=>{
            this.logMessage = '';
        }));
    }

    ngAfterViewInit(): void {
        // needed to get the app-div-auto-scroller to work
        window.setTimeout(()=>{
            window.dispatchEvent(new Event('resize'));
        },0);
    }

    private timeoutId: number
    @ViewChild('log') 
    logDiv: ElementRef | undefined;

    logMessage: string
}
