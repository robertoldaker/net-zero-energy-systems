import { Component, OnInit } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { ClassificationToolInputComponent } from '../classification-tool-input/classification-tool-input.component';
import { SubstationMount } from '../../data/app.data';

@Component({
    selector: 'app-classification-tool-dialog',
    templateUrl: './classification-tool-dialog.component.html',
    styleUrls: ['./classification-tool-dialog.component.css']
})
export class ClassificationToolDialogComponent implements OnInit {

    constructor(public dialogRef: MatDialogRef<ClassificationToolDialogComponent>) {
        this.input = {
            "elexonProfile": [],
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

    input: any

    close() {
        ;
    }

}
