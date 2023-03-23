import { Component } from '@angular/core';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.css']
})
export class HomeComponent {
    splitStart() {
        // this gets read by e-charts wrapper which will react to this and redraw
        window.dispatchEvent(new Event('resize'));
    }
    splitEnd() {
        // this gets read by e-charts wrapper which will react to this and redraw
        window.dispatchEvent(new Event('resize'));
    }
}
