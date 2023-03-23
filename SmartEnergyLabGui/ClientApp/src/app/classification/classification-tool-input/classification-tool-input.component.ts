import { Component, OnInit } from '@angular/core';
import { ClassificationToolInput, SubstationMount } from '../../data/app.data';
import { ClassificationToolService } from '../classification-tool.service';

@Component({
    selector: 'app-classification-tool-input',
    templateUrl: './classification-tool-input.component.html',
    styleUrls: ['./classification-tool-input.component.css']
})
export class ClassificationToolInputComponent implements OnInit {

    input: ClassificationToolInput

    constructor( private service: ClassificationToolService) {
        this.input = {
            "elexonProfile": [
              233,22,7,5,0,0,0,1
            ],
            "substationMount": SubstationMount.Ground,
            "transformerRating": 500,
            "percentIndustrialCustomers": 16,
            "numberOfFeeders": 3,
            "percentageHalfHourlyLoad": 0,
            "totalLength": 1.404,
            "percentageOverhead": 0
          }
    }


    ngOnInit(): void {
    }

    run() {
        this.service.run(this.input)
    }

}
