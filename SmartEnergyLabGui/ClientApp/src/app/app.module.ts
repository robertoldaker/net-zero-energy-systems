// basic angular stuff
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';

// angular material design
import { MatSliderModule } from '@angular/material/slider';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select'
import { MatIconModule } from '@angular/material/icon'; 
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';  
import { MatButtonToggleModule } from '@angular/material/button-toggle'; 
import { MatRadioModule } from '@angular/material/radio'; 
import { MatDividerModule } from '@angular/material/divider'; 
import { MatDialogModule } from '@angular/material/dialog'; 
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressBarModule } from '@angular/material/progress-bar'; 
import { MatAutocompleteModule } from '@angular/material/autocomplete'; 
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTabsModule } from '@angular/material/tabs'; 
import { MatExpansionModule} from '@angular/material/expansion'; 
import { MatGridListModule } from '@angular/material/grid-list'; 
import { MatListModule } from '@angular/material/list'; 
import { MatTableModule } from '@angular/material/table'; 
import { MatSortModule } from '@angular/material/sort';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner'; 
import { MatDatepickerModule } from '@angular/material/datepicker'; 
import { MAT_DATE_LOCALE, MatNativeDateModule } from '@angular/material/core';
import {MatPaginatorModule} from '@angular/material/paginator'; 

// google maps
import { GoogleMapsModule } from '@angular/google-maps'

// eCharts
import { NgxEchartsModule } from 'ngx-echarts';

// Angular split
import { AngularSplitModule } from 'angular-split';

// cookies
import { CookieService } from 'ngx-cookie-service';


// app imports
import { AppComponent } from './app.component';
import { ComponentBase } from './utils/component-base'
import { DialogBase } from './dialogs/dialog-base'
import { MainHeaderComponent } from './low-voltage/main-header/main-header.component';
import { HomeComponent } from './low-voltage/home/home.component';
import { MapComponent } from './low-voltage/map/map.component';
import { LoadProfilesComponent } from './low-voltage/load-profiles/load-profiles.component';
import { ClassificationInfoComponent } from './classification/classification-info/classification-info.component';
import { EChartsWrapperComponent } from './utils/e-charts-wrapper/e-charts-wrapper.component';
import { ClassificationToolComponent } from './classification/classification-tool/classification-tool.component';
import { ClassificationToolInputComponent } from './classification/classification-tool-input/classification-tool-input.component';
import { ClassificationToolLoadComponent } from './classification/classification-tool-load/classification-tool-load.component';
import { ClassificationToolClusterProbabilitiesComponent } from './classification/classification-tool-cluster-probabilities/classification-tool-cluster-probabilities.component';
import { ClassificationToolResultsComponent } from './classification/classification-tool-results/classification-tool-results.component';
import { ShowMessageComponent } from './main/show-message/show-message.component';
import { ClassificationToolDialogComponent } from './classification/classification-tool-dialog/classification-tool-dialog.component';
import { DialogHeaderComponent } from './dialogs/dialog-header/dialog-header.component';
import { DialogFooterComponent } from './dialogs/dialog-footer/dialog-footer.component';
import { StatusMessageComponent } from './main/status-message/status-message.component';
import { SignalRStatusComponent } from './main/signal-r-status/signal-r-status.component';
import { PrimaryInfoWindowComponent } from './low-voltage/primary-info-window/primary-info-window.component';
import { DistInfoWindowComponent } from './low-voltage/dist-info-window/dist-info-window.component';
import { AreaInfoWindowComponent } from './low-voltage/area-info-window/area-info-window.component';
import { DistSubstationDialogComponent } from './low-voltage/dist-substation-dialog/dist-substation-dialog.component';
import { AboutDialogComponent } from './main/about-dialog/about-dialog.component';
import { MapMarkerComponent } from './low-voltage/map-marker/map-marker.component';
import { ChargingInfoWindowComponent } from './low-voltage/charging-info-window/charging-info-window.component';
import { MapPowerComponent } from './low-voltage/map-power/map-power.component';
import { MapEvComponent } from './low-voltage/map-ev/map-ev.component';
import { MapHpComponent } from './low-voltage/map-hp/map-hp.component';
import { LoadProfileComponent } from './low-voltage/load-profile/load-profile.component';
import { LoadflowHomeComponent } from './loadflow/loadflow-home/loadflow-home.component';
import { LoadflowHeaderComponent } from './loadflow/loadflow-header/loadflow-header.component';
import { MainMenuComponent } from './main/main-menu/main-menu.component';
import { LoadflowDataComponent } from './loadflow/data/loadflow-data/loadflow-data.component';
import { LoadflowDialogComponent } from './loadflow/loadflow-dialog/loadflow-dialog.component';
import { LoadflowStagesComponent } from './loadflow/loadflow-stages/loadflow-stages.component';
import { LoadflowDataNodesComponent } from './loadflow/data/loadflow-data-nodes/loadflow-data-nodes.component';
import { LoadflowDataBranchesComponent } from './loadflow/data/loadflow-data-branches/loadflow-data-branches.component';
import { LoadflowDataCtrlsComponent } from './loadflow/data/loadflow-data-ctrls/loadflow-data-ctrls.component';
import { AboutLoadflowDialogComponent } from './loadflow/about-loadflow-dialog/about-loadflow-dialog.component';
import { LoadflowTripResultsComponent } from './loadflow/loadflow-trip-results/loadflow-trip-results.component';
import { LoadflowTripTableComponent } from './loadflow/loadflow-trip-table/loadflow-trip-table.component';
import { LoadflowHelpDialogComponent } from './loadflow/loadflow-help-dialog/loadflow-help-dialog.component';
import { ElsiHomeComponent } from './elsi/elsi-home/elsi-home.component';
import { ElsiDialogComponent } from './elsi/elsi-dialog/elsi-dialog.component';
import { ElsiResultsComponent } from './elsi/data/elsi-results/elsi-results.component';
import { ElsiHeaderComponent } from './elsi/elsi-header/elsi-header.component';
import { ElsiLogComponent } from './elsi/elsi-log/elsi-log.component';
import { ElsiInputsComponent } from './elsi/elsi-inputs/elsi-inputs.component';
import { ElsiOutputsComponent } from './elsi/data/elsi-outputs/elsi-outputs.component';
import { ElsiRowExpanderComponent } from './elsi/data/elsi-outputs/elsi-row-expander/elsi-row-expander.component';
import { ElsiDayControlComponent } from './elsi/data/elsi-outputs/elsi-day-control/elsi-day-control.component';
import { RegisterUserComponent } from './users/register-user/register-user.component';
import { UserHeaderComponent } from './users/user-header/user-header.component';
import { LogOnComponent } from './users/log-on/log-on.component';
import { HttpRequestInterceptor } from './data/HttpRequestInterceptor';
import { ChangePasswordComponent } from './users/change-password/change-password.component';
import { ElsiDemandsComponent } from './elsi/data/elsi-demands/elsi-demands.component';
import { ElsiGenerationComponent } from './elsi/data/elsi-generation/elsi-generation.component';
import { MessageDialogComponent } from './dialogs/message-dialog/message-dialog.component';
import { ElsiGenParametersComponent } from './elsi/data/elsi-gen-parameters/elsi-gen-parameters.component';
import { ElsiGenCapacitiesComponent } from './elsi/data/elsi-gen-capacities/elsi-gen-capacities.component';
import { CellEditorComponent } from './datasets/cell-editor/cell-editor.component';
import { ElsiMiscParamsComponent } from './elsi/data/elsi-misc-params/elsi-misc-params.component';
import { MatInputEditorComponent } from './datasets/mat-input-editor/mat-input-editor.component';
import { ElsiLinksComponent } from './elsi/data/elsi-links/elsi-links.component';
import { AboutElsiDialogComponent } from './elsi/about-elsi-dialog/about-elsi-dialog.component';
import { ElsiHelpDialogComponent } from './elsi/elsi-help-dialog/elsi-help-dialog.component';
import { ServiceWorkerModule } from '@angular/service-worker';
import { environment } from '../environments/environment';
import { GspInfoWindowComponent } from './low-voltage/gsp-info-window/gsp-info-window.component';
import { MapKeyComponent } from './low-voltage/map/map-key/map-key.component';
import { AdminHomeComponent } from './admin/admin-home/admin-home.component';
import { AdminUsersComponent } from './admin/admin-users/admin-users.component';
import { AdminGeneralComponent } from './admin/admin-general/admin-general.component';
import { AdminLogsComponent } from './admin/admin-logs-home/admin-logs/admin-logs.component';
import { AdminHeaderComponent } from './admin/admin-home/admin-header/admin-header.component';
import { AdminDataComponent } from './admin/admin-data/admin-data.component';
import { AdminTestComponent } from './admin/admin-test/admin-test.component';
import { LoadflowMapComponent } from './loadflow/loadflow-map/loadflow-map.component';
import { LoadflowMapKeyComponent } from './loadflow/loadflow-map/loadflow-map-key/loadflow-map-key.component';
import { LoadflowLocInfoWindowComponent } from './loadflow/loadflow-map/loadflow-loc-info-window/loadflow-loc-info-window.component';
import { LoadflowBranchInfoWindowComponent } from './loadflow/loadflow-map/loadflow-branch-info-window/loadflow-branch-info-window.component';
import { MaintenanceOverlayComponent } from './admin/maintenance-overlay/maintenance-overlay.component';
import { AdminLogsHomeComponent } from './admin/admin-logs-home/admin-logs-home.component';
import { NeedsLogonComponent } from './main/main-menu/needs-logon/needs-logon.component';
import { ResetPasswordComponent } from './users/reset-password/reset-password.component';
import { SolarInstallationsComponent } from './low-voltage/solar-installations/solar-installations.component';
import { DatasetSelectorComponent } from './datasets/dataset-selector/dataset-selector.component';
import { DatasetDialogComponent } from './datasets/dataset-dialog/dataset-dialog.component';
import { TablePaginatorComponent } from './datasets/table-paginator/table-paginator.component';
import { LoadflowDataBoundariesComponent } from './loadflow/data/loadflow-data-boundaries/loadflow-data-boundaries.component';
import { LoadflowDataZonesComponent } from './loadflow/data/loadflow-data-zones/loadflow-data-zones.component';
import { LoadflowNodeDialogComponent } from './loadflow/dialogs/loadflow-node-dialog/loadflow-node-dialog.component';
import { LoadflowMapSearchComponent } from './loadflow/loadflow-map/loadflow-map-search/loadflow-map-search.component';
import { DialogTextInputComponent } from './datasets/dialog-text-input/dialog-text-input.component';
import { DialogCheckboxComponent } from './datasets/dialog-checkbox/dialog-checkbox.component';
import { DialogSelectorComponent } from './datasets/dialog-selector/dialog-selector.component';
import { DialogBaseInput } from './datasets/dialog-base-input';
import { CellButtonsComponent } from './datasets/cell-buttons/cell-buttons.component';
import { LoadflowZoneDialogComponent } from './loadflow/dialogs/loadflow-zone-dialog/loadflow-zone-dialog.component';
import { LoadflowBoundaryDialogComponent } from './loadflow/dialogs/loadflow-boundary-dialog/loadflow-boundary-dialog.component';
import { LoadflowBranchDialogComponent } from './loadflow/dialogs/loadflow-branch-dialog/loadflow-branch-dialog.component';
import { LoadflowCtrlDialogComponent } from './loadflow/dialogs/loadflow-ctrl-dialog/loadflow-ctrl-dialog.component';
import { DialogAutoCompleteComponent } from './datasets/dialog-auto-complete/dialog-auto-complete.component';
import { LoadflowDataLocationsComponent } from './loadflow/data/loadflow-data-locations/loadflow-data-locations.component';
import { LoadflowLocationDialogComponent } from './loadflow/dialogs/loadflow-location-dialog/loadflow-location-dialog.component';
import { DivAutoScrollerComponent } from './utils/div-auto-scroller/div-auto-scroller.component';
import { DataTableBaseComponent } from './datasets/data-table-base/data-table-base.component';
import { MapButtonsComponent } from './datasets/map-buttons/map-buttons.component';
import { MiniMapButtonComponent } from './utils/mini-map-button/mini-map-button.component';
import { LoadflowMapButtonsComponent } from './loadflow/loadflow-map/loadflow-map-buttons/loadflow-map-buttons.component';
import { MiniIconButtonComponent } from './utils/mini-icon-button/mini-icon-button.component';
import { ColumnFilterComponent } from './datasets/column-filter/column-filter.component';
import { DistDataComponent } from './admin/admin-data/dist-data/dist-data.component';
import { TransDataComponent } from './admin/admin-data/trans-data/trans-data.component';


@NgModule({
    declarations: [
        AppComponent,
        ComponentBase,
        DialogBase,
        MainHeaderComponent,
        HomeComponent,
        MapComponent,
        LoadProfilesComponent,
        ClassificationInfoComponent,
        EChartsWrapperComponent,
        ClassificationToolComponent,
        ClassificationToolInputComponent,
        ClassificationToolLoadComponent,
        ClassificationToolClusterProbabilitiesComponent,
        ClassificationToolResultsComponent,
        ShowMessageComponent,
        ClassificationToolDialogComponent,
        DialogHeaderComponent,
        DialogFooterComponent,
        StatusMessageComponent,
        SignalRStatusComponent,
        PrimaryInfoWindowComponent,
        DistInfoWindowComponent,
        AreaInfoWindowComponent,
        DistSubstationDialogComponent,
        AboutDialogComponent,
        MapMarkerComponent,
        ChargingInfoWindowComponent,
        MapPowerComponent,
        MapEvComponent,
        MapHpComponent,
        LoadProfileComponent,
        LoadflowHomeComponent,
        LoadflowHeaderComponent,
        MainMenuComponent,
        LoadflowDataComponent,
        LoadflowDialogComponent,
        LoadflowStagesComponent,
        LoadflowDataNodesComponent,
        LoadflowDataBranchesComponent,
        LoadflowDataCtrlsComponent,
        AboutLoadflowDialogComponent,
        LoadflowTripResultsComponent,
        LoadflowTripTableComponent,
        LoadflowHelpDialogComponent,
        ElsiHomeComponent,
        ElsiDialogComponent,
        ElsiResultsComponent,
        ElsiHeaderComponent,
        ElsiLogComponent,
        ElsiInputsComponent,
        ElsiOutputsComponent,
        ElsiRowExpanderComponent,
        ElsiDayControlComponent,
        RegisterUserComponent,
        UserHeaderComponent,
        LogOnComponent,
        ChangePasswordComponent,
        ElsiDemandsComponent,
        ElsiGenerationComponent,
        MessageDialogComponent,
        ElsiGenParametersComponent,
        ElsiGenCapacitiesComponent,
        CellEditorComponent,
        ElsiMiscParamsComponent,
        MatInputEditorComponent,
        ElsiLinksComponent,
        AboutElsiDialogComponent,
        ElsiHelpDialogComponent,
        GspInfoWindowComponent,
        MapKeyComponent,
        AdminHomeComponent,
        AdminUsersComponent,
        AdminGeneralComponent,
        AdminLogsComponent,
        AdminHeaderComponent,
        AdminDataComponent,
        AdminTestComponent,
        LoadflowMapComponent,
        LoadflowMapKeyComponent,
        LoadflowLocInfoWindowComponent,
        LoadflowBranchInfoWindowComponent,
        MaintenanceOverlayComponent,
        AdminLogsHomeComponent,
        NeedsLogonComponent,
        ResetPasswordComponent,
        SolarInstallationsComponent,
        DatasetSelectorComponent,
        DatasetDialogComponent,
        TablePaginatorComponent,
        LoadflowDataBoundariesComponent,
        LoadflowDataZonesComponent,
        LoadflowNodeDialogComponent,
        LoadflowMapSearchComponent,
        DialogTextInputComponent,
        DialogCheckboxComponent,
        DialogSelectorComponent,
        DialogBaseInput,
        CellButtonsComponent,
        LoadflowZoneDialogComponent,
        LoadflowBoundaryDialogComponent,
        LoadflowBranchDialogComponent,
        LoadflowCtrlDialogComponent,
        DialogAutoCompleteComponent,
        LoadflowDataLocationsComponent,
        LoadflowLocationDialogComponent,
        DivAutoScrollerComponent,
        DataTableBaseComponent,
        MapButtonsComponent,
        MiniMapButtonComponent,
        LoadflowMapButtonsComponent,
        MiniIconButtonComponent,
        ColumnFilterComponent,
        DistDataComponent,
        TransDataComponent,
    ],
    imports: [
        BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
        HttpClientModule,
        FormsModule,
        ReactiveFormsModule,
        RouterModule.forRoot([
            //??{ path: '',   redirectTo: '/lowVoltage', pathMatch: 'full' }, // redirect to `first-component`
            { path: '',   redirectTo: '/loadflow', pathMatch: 'full' }, // redirect to `first-component`
            { path: 'bathLV',   redirectTo: '/lowVoltage', pathMatch: 'full' }, // redirect to `first-component`
            { path: 'lowVoltage', component: HomeComponent},
            { path: 'ResetPassword', component: HomeComponent},
            { path: 'solarInstallations', component: HomeComponent},
            { path: 'loadflow', component: LoadflowHomeComponent},
            { path: 'elsi', component: ElsiHomeComponent},
            { path: 'classificationTool', component: ClassificationToolComponent},
            { path: 'admin', component: AdminHomeComponent}
        ]),
        NgxEchartsModule.forRoot({
            /**
             * This will import all modules from echarts.
             * If you only need custom modules,
             * please refer to [Custom Build] section.
             */
            echarts: () => import('echarts'), // or import('./path-to-my-custom-echarts')
          }),
        BrowserAnimationsModule,
        MatSliderModule,
        MatInputModule,
        MatSelectModule,
        GoogleMapsModule,
        MatIconModule,
        MatMenuModule,
        MatButtonModule,
        MatButtonToggleModule,
        MatRadioModule,
        MatDividerModule,
        MatDialogModule,
        MatSnackBarModule,
        MatProgressBarModule,
        MatAutocompleteModule,
        MatCheckboxModule,
        MatTabsModule,
        AngularSplitModule,
        MatExpansionModule,
        MatGridListModule,
        MatListModule,
        MatTableModule,
        MatSortModule,
        ServiceWorkerModule.register('ngsw-worker.js', {
          enabled: environment.production,
          // Register the ServiceWorker as soon as the application is stable
          // or after 30 seconds (whichever comes first).
          registrationStrategy: 'registerWhenStable:30000'
        }),
        MatProgressSpinnerModule,
        MatDatepickerModule,
        MatNativeDateModule,
        MatPaginatorModule
    ],
    providers: [
        // Http Interceptor(s) -  adds with Client Credentials
        [
            { provide: HTTP_INTERCEPTORS, useClass: HttpRequestInterceptor, multi: true },
            { provide: MAT_DATE_LOCALE, useValue: 'en-GB'}
        ],
        [CookieService]
    ],
    bootstrap: [AppComponent]
})
export class AppModule { }
