import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { ElsiDataService } from '../../elsi-data.service';

@Component({
    selector: 'app-elsi-generation',
    templateUrl: './elsi-generation.component.html',
    styleUrls: ['./elsi-generation.component.css']
})
export class ElsiGenerationComponent extends ComponentBase implements OnInit, AfterViewInit {

    constructor(public service: ElsiDataService) {
        super()
        
    }

    ngAfterViewInit(): void {
        
    }

    ngOnInit(): void {

    }

    panelOpenState:boolean = false;

    tabChange(e: any) {
        // dispatch this so that app-div-auto-scroller can detect size change
        window.dispatchEvent(new Event('resize'));
    }

}
