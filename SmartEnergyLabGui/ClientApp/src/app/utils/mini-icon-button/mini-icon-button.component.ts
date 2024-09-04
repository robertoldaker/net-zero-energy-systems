import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
    selector: 'app-mini-icon-button',
    templateUrl: './mini-icon-button.component.html',
    styleUrls: ['./mini-icon-button.component.css']
})
export class MiniIconButtonComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
    }

    @Input()
    disabled = false
    
    @Input()
    iconName = 'edit' 
    
    @Input()
    fontSize = "inherit"

    @Input()
    title = ''
    
    click(e: any) {  
        if ( this.disabled) {
            e.stopPropagation()
        }
    }
}
