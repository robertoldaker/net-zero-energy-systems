import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Subscription } from 'rxjs';
import { ElsiDataService } from '../elsi-data.service';
import { ComponentBase } from 'src/app/utils/component-base';
import { DivAutoScrollerComponent } from 'src/app/utils/div-auto-scroller/div-auto-scroller.component';

@Component({
    selector: 'app-elsi-log',
    templateUrl: './elsi-log.component.html',
    styleUrls: ['./elsi-log.component.css']
})
export class ElsiLogComponent extends ComponentBase implements AfterViewInit {

    constructor(private service: ElsiDataService) {
        super()
        this.logMessage = ''
        this.addSub(service.LogMessageAvailable.subscribe((log) => {
            this.logMessage += log + '\n';
            if ( this.autoScroller ) {
                // need a timeout since this event is fired lots during a calculation run
                this.autoScroller.scrollBottom(1000)
            }
        }));
        this.addSub(service.RunStart.subscribe(() => {
            this.logMessage = '';
        }));
    }

    ngAfterViewInit(): void {
        // needed to get the app-div-auto-scroller to work
        window.setTimeout(() => {
            window.dispatchEvent(new Event('resize'));
        }, 0);
    }

    @ViewChild(DivAutoScrollerComponent)
    autoScroller: DivAutoScrollerComponent | undefined;


    logMessage: string
}
