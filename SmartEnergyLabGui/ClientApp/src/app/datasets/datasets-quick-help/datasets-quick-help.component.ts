import { Component, OnInit } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { QuickHelpService } from 'src/app/utils/quick-help/quick-help.service';

@Component({
  selector: 'app-datasets-quick-help',
  templateUrl: './datasets-quick-help.component.html',
  styleUrls: ['./datasets-quick-help.component.css']
})
export class DatasetsQuickHelpComponent extends ComponentBase {

    constructor(private service: QuickHelpService) {
        super()
    }

    get helpId():string {
        return this.service.helpId
    }
}
