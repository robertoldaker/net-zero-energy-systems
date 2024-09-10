import { Component, Input, OnInit } from '@angular/core';
import { ColumnDataFilter } from '../cell-editor/cell-editor.component';

@Component({
    selector: 'app-column-filter',
    templateUrl: './column-filter.component.html',
    styleUrls: ['./column-filter.component.css']
})
export class ColumnFilterComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
    }

    @Input()
    title: string  = "?"

    @Input()
    filter: ColumnDataFilter | undefined

    selectOption(e: any, v: any) {
        this.filter?.enable(v)
    }

    menuClick(e: any) {
        e.stopPropagation()
    }

    disable(e: any) {
        this.filter?.disable()
        e.stopPropagation()
    }

}
