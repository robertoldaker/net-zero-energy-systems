import { Component, Input, OnInit } from '@angular/core';
import { GeneratorType } from 'src/app/data/app.data';

@Component({
    selector: 'app-loadflow-gen-svgs',
    templateUrl: './loadflow-gen-svgs.component.html',
    styleUrls: ['./loadflow-gen-svgs.component.css']
})
export class LoadflowGenSvgsComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
    }

    @Input()
    genType: GeneratorType = GeneratorType.Nuclear
    GeneratorType = GeneratorType

    @Input()
    width: string = "30px"
}
