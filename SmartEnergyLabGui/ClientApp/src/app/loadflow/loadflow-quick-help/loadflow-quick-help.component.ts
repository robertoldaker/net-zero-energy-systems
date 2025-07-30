import { Component, OnInit } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { QuickHelpService } from 'src/app/utils/quick-help/quick-help.service';

@Component({
    selector: 'app-loadflow-quick-help',
    templateUrl: './loadflow-quick-help.component.html',
    styleUrls: ['./loadflow-quick-help.component.css']
})
export class LoadflowQuickHelpComponent extends ComponentBase {

    constructor(private service: QuickHelpService) {
        super()
    }

    get helpId():string {
        return this.service.helpId
    }

}
