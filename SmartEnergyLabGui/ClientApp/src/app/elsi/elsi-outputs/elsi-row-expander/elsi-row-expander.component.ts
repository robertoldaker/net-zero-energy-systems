import { Component, Input, OnInit } from '@angular/core';

export enum ElsiRowExpanderSize { 
    Large = 'large', 
    Medium = 'medium', 
    Small = 'small'
}

@Component({
    selector: '[elsiRowExpander]',
    templateUrl: './elsi-row-expander.component.html',
    styleUrls: ['./elsi-row-expander.component.css']
})
export class ElsiRowExpanderComponent implements OnInit {

    constructor() {
        this.title1 = ''
        this.title2 = ''
        this.title3 = ''
        this.expanded = true
        this.size = ElsiRowExpanderSize.Medium
    }

    ngOnInit(): void {
    }

    @Input()
    title1: string
    @Input()
    title2: string
    @Input()
    title3: string

    @Input()
    size: ElsiRowExpanderSize

    expanded: boolean

    toggleExpanded() {
        this.expanded=!this.expanded;
    }

    get icon() {
        return this.expanded ? "keyboard_arrow_up" : "keyboard_arrow_down"
    }

    get buttonClass():string {
        return this.size.valueOf();
    }    
}
