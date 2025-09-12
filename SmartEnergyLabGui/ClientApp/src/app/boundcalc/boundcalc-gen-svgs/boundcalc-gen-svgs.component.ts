import { Component, Input, OnInit } from '@angular/core';
import { GeneratorType } from 'src/app/data/app.data';

@Component({
    selector: 'app-boundcalc-gen-svgs',
    templateUrl: './boundcalc-gen-svgs.component.html',
    styleUrls: ['./boundcalc-gen-svgs.component.css']
})
export class BoundCalcGenSvgsComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
    }

    @Input()
    genType: GeneratorType = GeneratorType.Nuclear
    GeneratorType = GeneratorType

    @Input()
    width: string = "30px"
}
