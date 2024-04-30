import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { MapPowerService } from '../map-power.service';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
    constructor(private route: ActivatedRoute, private dialogService: DialogService, public mapPowerService: MapPowerService) {
    }
    ngOnInit() {
        let route = this.route.snapshot
        if ( route.url.length>0 && route.url[0].path=="ResetPassword") {
            let token = this.route.snapshot.queryParamMap.get('token');
            console.log(`token=${token}`)
            console.log(this.route.snapshot)
            if ( token!=null ) {
                this.dialogService.showResetPasswordDialog(token);
            }
        }
    }
    splitStart() {
        // this gets read by e-charts wrapper which will react to this and redraw
        window.dispatchEvent(new Event('resize'));
    }
    splitEnd() {
        // this gets read by e-charts wrapper which will react to this and redraw
        window.dispatchEvent(new Event('resize'));
    }
}
