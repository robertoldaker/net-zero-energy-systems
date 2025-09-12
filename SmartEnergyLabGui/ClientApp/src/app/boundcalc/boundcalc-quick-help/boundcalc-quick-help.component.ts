import { Component, OnInit } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { QuickHelpService } from 'src/app/utils/quick-help/quick-help.service';

@Component({
    selector: 'app-boundcalc-quick-help',
    templateUrl: './boundcalc-quick-help.component.html',
    styleUrls: ['./boundcalc-quick-help.component.css']
})
export class BoundCalcQuickHelpComponent extends ComponentBase {

    constructor(private service: QuickHelpService) {
        super()
    }

    get helpId():string {
        return this.service.helpId
    }

}
