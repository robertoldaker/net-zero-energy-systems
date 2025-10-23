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
    elexonInfo: string[]

    constructor( public service: ClassificationToolService) {
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
          this.elexonInfo = [
            "Domestic Unrestricted Customers",
            "Domestic Economy 7 Customers",
            "Non-Domestic Unrestricted Customers",
            "Non-Domestic Economy 7 Customers",
            "Non-Domestic Maximum Demand (MD) Customers with a Peak Load Factor (LF) of less than 20%",
            "Non-Domestic Maximum Demand Customers with a Peak Load Factor between 20% and 30%",
            "Non-Domestic Maximum Demand Customers with a Peak Load Factor between 30% and 40%",
            "Non-Domestic Maximum Demand Customers with a Peak Load Factor over 40%"
          ]
    }


    ngOnInit(): void {
    }

    run() {
        for(var i=0;i<this.input.elexonProfile.length;i++) {
            var ep = this.input.elexonProfile[i];
            if ( typeof(ep) === 'string') {
                this.input.elexonProfile[i] = parseFloat(ep)
            }
        }
        this.input.substationMount = Number(this.input.substationMount)
        this.input.transformerRating = Number(this.input.transformerRating)
        this.input.percentIndustrialCustomers = Number(this.input.percentIndustrialCustomers)
        this.input.percentageHalfHourlyLoad = Number(this.input.percentageHalfHourlyLoad)
        this.input.percentageOverhead = Number(this.input.percentageOverhead)
        this.input.numberOfFeeders = Number(this.input.numberOfFeeders)
        this.input.totalLength = Number(this.input.totalLength)
        this.service.run(this.input)
    }

}
