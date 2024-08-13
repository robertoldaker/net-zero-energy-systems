import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
    selector: 'app-mini-map-button',
    templateUrl: './mini-map-button.component.html',
    styleUrls: ['./mini-map-button.component.css']
})
export class MiniMapButtonComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
    }

    @Input()
    disabled = false

    @Output()
    onClick: EventEmitter<any> = new EventEmitter()

    click(e: any) {
        if ( !this.disabled ) {
            this.onClick.emit(e)
        }
    }

}
