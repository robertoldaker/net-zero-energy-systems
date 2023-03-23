import { Component, OnInit } from '@angular/core';
import { SwUpdate, VersionEvent } from '@angular/service-worker';
import { interval } from 'rxjs';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
    title = 'app';

    constructor( private swUpdate: SwUpdate ) {
    }

    ngOnInit(): void {
        // check for platform update
        if (this.swUpdate.isEnabled) {
            this.swUpdate.checkForUpdate();
            interval(60000).subscribe(() => this.swUpdate.checkForUpdate().then(() => {
                // checking for updates
            }));
        }
        this.swUpdate.versionUpdates.subscribe((e: VersionEvent) => {
            if ( e.type == "VERSION_READY") {
                location.reload();
            }
        });
    }

}
