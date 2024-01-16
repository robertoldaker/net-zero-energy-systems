import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MainService } from 'src/app/main/main.service';

@Component({
    selector: 'app-maintenance-overlay',
    templateUrl: './maintenance-overlay.component.html',
    styleUrls: ['./maintenance-overlay.component.css'],
})
export class MaintenanceOverlayComponent {
    constructor(public mainService: MainService) {
        
    }
}
