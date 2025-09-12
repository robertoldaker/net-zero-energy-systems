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
import { BoundCalcHomeComponent } from './boundcalc/boundcalc-home/boundcalc-home.component';
import { BoundCalcHeaderComponent } from './boundcalc/boundcalc-header/boundcalc-header.component';
import { MainMenuComponent } from './main/main-menu/main-menu.component';
import { BoundCalcDataComponent } from './boundcalc/data/boundcalc-data/boundcalc-data.component';
import { BoundCalcDialogComponent } from './boundcalc/boundcalc-dialog/boundcalc-dialog.component';
import { BoundCalcStagesComponent } from './boundcalc/boundcalc-stages/boundcalc-stages.component';
import { BoundCalcDataNodesComponent } from './boundcalc/data/boundcalc-data-nodes/boundcalc-data-nodes.component';
import { BoundCalcDataBranchesComponent } from './boundcalc/data/boundcalc-data-branches/boundcalc-data-branches.component';
import { BoundCalcDataCtrlsComponent } from './boundcalc/data/boundcalc-data-ctrls/boundcalc-data-ctrls.component';
import { AboutBoundCalcDialogComponent } from './boundcalc/about-boundcalc-dialog/about-boundcalc-dialog.component';
import { BoundCalcTripResultsComponent } from './boundcalc/boundcalc-trip-results/boundcalc-trip-results.component';
import { BoundCalcTripTableComponent } from './boundcalc/boundcalc-trip-table/boundcalc-trip-table.component';
import { BoundCalcHelpDialogComponent } from './boundcalc/boundcalc-help-dialog/boundcalc-help-dialog.component';
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
import { BoundCalcMapComponent } from './boundcalc/boundcalc-map/boundcalc-map.component';
import { BoundCalcMapKeyComponent } from './boundcalc/boundcalc-map/boundcalc-map-key/boundcalc-map-key.component';
import { BoundCalcLocInfoWindowComponent } from './boundcalc/boundcalc-map/boundcalc-loc-info-window/boundcalc-loc-info-window.component';
import { BoundCalcBranchInfoWindowComponent } from './boundcalc/boundcalc-map/boundcalc-branch-info-window/boundcalc-branch-info-window.component';
import { MaintenanceOverlayComponent } from './admin/maintenance-overlay/maintenance-overlay.component';
import { AdminLogsHomeComponent } from './admin/admin-logs-home/admin-logs-home.component';
import { NeedsLogonComponent } from './main/main-menu/needs-logon/needs-logon.component';
import { ResetPasswordComponent } from './users/reset-password/reset-password.component';
import { SolarInstallationsComponent } from './low-voltage/solar-installations/solar-installations.component';
import { DatasetSelectorComponent } from './datasets/dataset-selector/dataset-selector.component';
import { DatasetDialogComponent } from './datasets/dataset-dialog/dataset-dialog.component';
import { TablePaginatorComponent } from './datasets/table-paginator/table-paginator.component';
import { BoundCalcDataBoundariesComponent } from './boundcalc/data/boundcalc-data-boundaries/boundcalc-data-boundaries.component';
import { BoundCalcDataZonesComponent } from './boundcalc/data/boundcalc-data-zones/boundcalc-data-zones.component';
import { BoundCalcNodeDialogComponent } from './boundcalc/dialogs/boundcalc-node-dialog/boundcalc-node-dialog.component';
import { BoundCalcMapSearchComponent } from './boundcalc/boundcalc-map/boundcalc-map-search/boundcalc-map-search.component';
import { DialogTextInputComponent } from './datasets/dialog-text-input/dialog-text-input.component';
import { DialogCheckboxComponent } from './datasets/dialog-checkbox/dialog-checkbox.component';
import { DialogSelectorComponent } from './datasets/dialog-selector/dialog-selector.component';
import { DialogBaseInput } from './datasets/dialog-base-input';
import { CellButtonsComponent } from './datasets/cell-buttons/cell-buttons.component';
import { BoundCalcZoneDialogComponent } from './boundcalc/dialogs/boundcalc-zone-dialog/boundcalc-zone-dialog.component';
import { BoundCalcBoundaryDialogComponent } from './boundcalc/dialogs/boundcalc-boundary-dialog/boundcalc-boundary-dialog.component';
import { BoundCalcBranchDialogComponent } from './boundcalc/dialogs/boundcalc-branch-dialog/boundcalc-branch-dialog.component';
import { BoundCalcCtrlDialogComponent } from './boundcalc/dialogs/boundcalc-ctrl-dialog/boundcalc-ctrl-dialog.component';
import { DialogAutoCompleteComponent } from './datasets/dialog-auto-complete/dialog-auto-complete.component';
import { BoundCalcDataLocationsComponent } from './boundcalc/data/boundcalc-data-locations/boundcalc-data-locations.component';
import { BoundCalcLocationDialogComponent } from './boundcalc/dialogs/boundcalc-location-dialog/boundcalc-location-dialog.component';
import { DivAutoScrollerComponent } from './utils/div-auto-scroller/div-auto-scroller.component';
import { DataTableBaseComponent } from './datasets/data-table-base/data-table-base.component';
import { MapButtonsComponent } from './datasets/map-buttons/map-buttons.component';
import { MiniMapButtonComponent } from './utils/mini-map-button/mini-map-button.component';
import { BoundCalcMapButtonsComponent } from './boundcalc/boundcalc-map/boundcalc-map-buttons/boundcalc-map-buttons.component';
import { MiniIconButtonComponent } from './utils/mini-icon-button/mini-icon-button.component';
import { ColumnFilterComponent } from './datasets/column-filter/column-filter.component';
import { DistDataComponent } from './admin/admin-data/dist-data/dist-data.component';
import { TransDataComponent } from './admin/admin-data/trans-data/trans-data.component';
import { BoundCalcNodeCellComponent } from './boundcalc/data/boundcalc-node-cell/boundcalc-node-cell.component';
import { BoundCalcBranchCodeCellComponent } from './boundcalc/data/boundcalc-branch-code-cell/boundcalc-branch-code-cell.component';
import { MapAdvancedMarker } from './boundcalc/boundcalc-map/map-advanced-marker/map-advanced-marker';
import { BoundCalcTripLinkComponent } from './boundcalc/boundcalc-trip-table/boundcalc-trip-link/boundcalc-trip-link.component';
import { BoundCalcTripCellComponent } from './boundcalc/boundcalc-trip-table/boundcalc-trip-cell/boundcalc-trip-cell.component';
import { BoundCalcBranchInfoTableComponent } from './boundcalc/boundcalc-map/boundcalc-branch-info-table/boundcalc-branch-info-table.component';
import { BoundCalcNodeInfoTableComponent } from './boundcalc/boundcalc-map/boundcalc-node-info-table/boundcalc-node-info-table.component';
import { BoundCalcDataGeneratorsComponent } from './boundcalc/data/boundcalc-data-generators/boundcalc-data-generators.component';
import { BoundCalcDataGenerationModelsComponent } from './boundcalc/data/boundcalc-data-generation-models/boundcalc-data-generation-models.component';
import { BoundCalcGenerationModelDialogComponent } from './boundcalc/dialogs/boundcalc-generation-model-dialog/boundcalc-generation-model-dialog.component';
import { BoundCalcGeneratorDialogComponent } from './boundcalc/dialogs/boundcalc-generator-dialog/boundcalc-generator-dialog.component';
import { GspHomeComponent } from './gsp-demand-profiles/gsp-home/gsp-home.component';
import { GspHeaderComponent } from './gsp-demand-profiles/gsp-header/gsp-header.component';
import { GspMapComponent } from './gsp-demand-profiles/gsp-map/gsp-map.component';
import { GspDemandProfilesComponent } from './gsp-demand-profiles/gsp-demand-profiles/gsp-demand-profiles.component';
import { BoundCalcInfoWindowComponent } from './boundcalc/boundcalc-map/boundcalc-info-window/boundcalc-info-window.component';
import { GspSearchComponent } from './gsp-demand-profiles/gsp-search/gsp-search.component';
import { BoundCalcGenSvgsComponent } from './boundcalc/boundcalc-gen-svgs/boundcalc-gen-svgs.component';
import { BoundCalcDataCtrlsNodeZoneComponent } from './boundcalc/data/boundcalc-data-ctrls/boundcalc-data-ctrls-node-zone/boundcalc-data-ctrls-node-zone.component';
import { QuickHelpComponent } from './utils/quick-help/quick-help.component';
import { QuickHelpContentComponent } from './utils/quick-help-content/quick-help-content.component';
import { BoundCalcQuickHelpComponent } from './boundcalc/boundcalc-quick-help/boundcalc-quick-help.component';
import { DatasetsQuickHelpComponent } from './datasets/datasets-quick-help/datasets-quick-help.component';
import { QuickHelpDialogGroupComponent } from './utils/quick-help-dialog-group/quick-help-dialog-group.component';
import { PathNotFoundComponent } from './main/path-not-found/path-not-found.component';


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
        BoundCalcHomeComponent,
        BoundCalcHeaderComponent,
        MainMenuComponent,
        BoundCalcDataComponent,
        BoundCalcDialogComponent,
        BoundCalcStagesComponent,
        BoundCalcDataNodesComponent,
        BoundCalcDataBranchesComponent,
        BoundCalcDataCtrlsComponent,
        AboutBoundCalcDialogComponent,
        BoundCalcTripResultsComponent,
        BoundCalcTripTableComponent,
        BoundCalcHelpDialogComponent,
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
        BoundCalcMapComponent,
        BoundCalcMapKeyComponent,
        BoundCalcLocInfoWindowComponent,
        BoundCalcBranchInfoWindowComponent,
        MaintenanceOverlayComponent,
        AdminLogsHomeComponent,
        NeedsLogonComponent,
        ResetPasswordComponent,
        SolarInstallationsComponent,
        DatasetSelectorComponent,
        DatasetDialogComponent,
        TablePaginatorComponent,
        BoundCalcDataBoundariesComponent,
        BoundCalcDataZonesComponent,
        BoundCalcNodeDialogComponent,
        BoundCalcMapSearchComponent,
        DialogTextInputComponent,
        DialogCheckboxComponent,
        DialogSelectorComponent,
        DialogBaseInput,
        CellButtonsComponent,
        BoundCalcZoneDialogComponent,
        BoundCalcBoundaryDialogComponent,
        BoundCalcBranchDialogComponent,
        BoundCalcCtrlDialogComponent,
        DialogAutoCompleteComponent,
        BoundCalcDataLocationsComponent,
        BoundCalcLocationDialogComponent,
        DivAutoScrollerComponent,
        DataTableBaseComponent,
        MapButtonsComponent,
        MiniMapButtonComponent,
        BoundCalcMapButtonsComponent,
        MiniIconButtonComponent,
        ColumnFilterComponent,
        DistDataComponent,
        TransDataComponent,
        BoundCalcNodeCellComponent,
        BoundCalcBranchCodeCellComponent,
        MapAdvancedMarker,
        BoundCalcTripLinkComponent,
        BoundCalcTripCellComponent,
        BoundCalcBranchInfoTableComponent,
        BoundCalcNodeInfoTableComponent,
        BoundCalcDataGeneratorsComponent,
        BoundCalcDataGenerationModelsComponent,
        BoundCalcGenerationModelDialogComponent,
        BoundCalcGeneratorDialogComponent,
        GspHomeComponent,
        GspHeaderComponent,
        GspMapComponent,
        GspDemandProfilesComponent,
        BoundCalcInfoWindowComponent,
        GspSearchComponent,
        BoundCalcGenSvgsComponent,
        BoundCalcDataCtrlsNodeZoneComponent,
        QuickHelpComponent,
        QuickHelpContentComponent,
        BoundCalcQuickHelpComponent,
        DatasetsQuickHelpComponent,
        QuickHelpDialogGroupComponent,
        PathNotFoundComponent
    ],
    imports: [
        BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
        HttpClientModule,
        FormsModule,
        ReactiveFormsModule,
        RouterModule.forRoot([
            { path: '',   redirectTo: '/boundCalc', pathMatch: 'full' },
            { path: 'bathLV',   redirectTo: '/lowVoltage', pathMatch: 'full' },
            { path: 'lowVoltage', component: HomeComponent},
            { path: 'ResetPassword', component: HomeComponent},
            { path: 'solarInstallations', component: HomeComponent},
            { path: 'loadflow', redirectTo: '/boundCalc' },
            { path: 'boundCalc', component: BoundCalcHomeComponent},
            { path: 'elsi', component: ElsiHomeComponent},
            { path: 'classificationTool', component: ClassificationToolComponent},
            { path: 'admin', component: AdminHomeComponent},
            { path: 'gspDemandProfiles', component: GspHomeComponent},
            // This is the wildcard route for a 404 page
            { path: '**', component: PathNotFoundComponent }

        ]
            ),
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
