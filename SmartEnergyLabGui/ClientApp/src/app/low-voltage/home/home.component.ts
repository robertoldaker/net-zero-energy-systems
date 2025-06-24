import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { MapPowerService } from '../map-power.service';
import { SolarInstallationsComponent } from '../solar-installations/solar-installations.component';
import { LoadProfilesComponent } from '../load-profiles/load-profiles.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { GISData } from 'src/app/data/app.data';
import { LogOnComponent } from 'src/app/users/log-on/log-on.component';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.css']
})
export class HomeComponent extends ComponentBase implements OnInit {
    constructor(private route: ActivatedRoute,
        private dialogService: DialogService,
        public mapPowerService: MapPowerService,
        titleService: Title) {
        super()
        titleService.setTitle("Low voltage")
    }

    @ViewChild(SolarInstallationsComponent)
    solarInstallationsCpnt: SolarInstallationsComponent | null = null
    @ViewChild(LoadProfilesComponent)
    loadProfilesCpnt: LoadProfilesComponent | null = null

    ngOnInit() {
        let route = this.route.snapshot
        if ( route.url.length>0 && route.url[0].path=="ResetPassword") {
            let token = this.route.snapshot.queryParamMap.get('token');
            if ( token!=null ) {
                this.dialogService.showResetPasswordDialog(token);
            }
        } else if ( route.url.length>0 && route.url[0].path == "solarInstallations" ) {
            this.mapPowerService.GridSupplyPointsMarkersReady.subscribe(()=>{
                window.setTimeout(()=>{
                    let gsp = this.mapPowerService.GridSupplyPoints.find( m=>m.name==="Melksham  S.G.P." );
                    if ( gsp ) {
                        this.mapPowerService.setSolarInstallationsMode(true);
                        this.mapPowerService.setSelectedGridSupplyPoint(gsp)
                        let gisData: GISData = {
                            id: 0,
                            latitude: gsp.gisData.latitude - 0.05,
                            longitude: gsp.gisData.longitude
                        }
                        this.mapPowerService.setPanTo(gisData,12);
                    }
                },0)
            })
        }

        //
        this.addSub( this.mapPowerService.SolarInstallationsModeChanged.subscribe( (mode)=>{
            this.tabIndex = mode ? 1 : 0
            if ( this.solarInstallationsCpnt && this.tabIndex===1 ) {
                this.solarInstallationsCpnt.redraw()
            }
            if ( this.loadProfilesCpnt && this.tabIndex===0 ) {
                this.loadProfilesCpnt.redraw()
            }
            }))
    }
    splitStart() {
        // this gets read by e-charts wrapper which will react to this and redraw
        window.dispatchEvent(new Event('resize'));
    }
    splitEnd() {
        // this gets read by e-charts wrapper which will react to this and redraw
        window.dispatchEvent(new Event('resize'));
    }

    onTabChange(e: any) {
        this.mapPowerService.setSolarInstallationsMode( e.index==1 ? true : false )
    }

    tabIndex: number = 0
}
