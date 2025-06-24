import { Component, OnInit } from '@angular/core';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'app-classification-tool',
    templateUrl: './classification-tool.component.html',
    styleUrls: ['./classification-tool.component.css']
})
export class ClassificationToolComponent implements OnInit {

    constructor(titleService: Title) {
        titleService.setTitle('Classification Tool')
    }

    ngOnInit(): void {
    }

}
