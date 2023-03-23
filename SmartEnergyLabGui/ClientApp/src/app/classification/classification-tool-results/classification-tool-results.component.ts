import { Component, OnDestroy, OnInit } from '@angular/core';
import { ClassificationToolOutput } from '../../data/app.data';
import { ClassificationToolService } from '../classification-tool.service';

@Component({
    selector: 'app-classification-tool-results',
    templateUrl: './classification-tool-results.component.html',
    styleUrls: ['./classification-tool-results.component.css']
})

export class ClassificationToolResultsComponent implements OnInit, OnDestroy {

    private subs1: any
    output: ClassificationToolOutput | undefined

    constructor(private service: ClassificationToolService) {
        this.subs1 = service.OutputLoaded.subscribe((output: ClassificationToolOutput)=> {
            this.output = output
        });
    }

    ngOnDestroy(): void {
        this.subs1.unsubscribe()
    }

    ngOnInit(): void {

    }

}
