import { Component, Injectable } from '@angular/core';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { AboutDialogComponent } from '../main/about-dialog/about-dialog.component';
import { AboutLoadflowDialogComponent } from '../loadflow/about-loadflow-dialog/about-loadflow-dialog.component';
import { ClassificationToolDialogComponent } from '../classification/classification-tool-dialog/classification-tool-dialog.component';
import { DistSubstationDialogComponent } from '../low-voltage/dist-substation-dialog/dist-substation-dialog.component';
import { LoadflowHelpDialogComponent } from '../loadflow/loadflow-help-dialog/loadflow-help-dialog.component';
import { LogOnComponent } from '../users/log-on/log-on.component';
import { RegisterUserComponent } from '../users/register-user/register-user.component';
import { DataClientService } from '../data/data-client.service';
import { MapDataService } from '../low-voltage/map-data.service';
import { ChangePasswordComponent } from '../users/change-password/change-password.component';
import { Dataset, Node, TransportModel, Zone, Generator } from '../data/app.data';
import { MessageDialog, MessageDialogComponent } from './message-dialog/message-dialog.component';
import { AboutElsiDialogComponent } from '../elsi/about-elsi-dialog/about-elsi-dialog.component';
import { ElsiHelpDialogComponent } from '../elsi/elsi-help-dialog/elsi-help-dialog.component';
import { NeedsLogonComponent } from '../main/main-menu/needs-logon/needs-logon.component';
import { ResetPasswordComponent } from '../users/reset-password/reset-password.component';
import { DatasetDialogComponent } from '../datasets/dataset-dialog/dataset-dialog.component';
import { LoadflowNodeDialogComponent } from '../loadflow/dialogs/loadflow-node-dialog/loadflow-node-dialog.component';
import { ICellEditorDataDict } from '../datasets/cell-editor/cell-editor.component';
import { LoadflowZoneDialogComponent } from '../loadflow/dialogs/loadflow-zone-dialog/loadflow-zone-dialog.component';
import { LoadflowBoundaryDialogComponent } from '../loadflow/dialogs/loadflow-boundary-dialog/loadflow-boundary-dialog.component';
import { LoadflowBranchDialogComponent } from '../loadflow/dialogs/loadflow-branch-dialog/loadflow-branch-dialog.component';
import { LoadflowCtrlDialogComponent } from '../loadflow/dialogs/loadflow-ctrl-dialog/loadflow-ctrl-dialog.component';
import { LoadflowLocationDialogComponent } from '../loadflow/dialogs/loadflow-location-dialog/loadflow-location-dialog.component';
import { IBranchEditorData } from '../loadflow/loadflow-data-service.service';
import { LoadflowTransportModelDialogComponent } from '../loadflow/dialogs/loadflow-transport-model-dialog/loadflow-transport-model-dialog.component';
import { LoadflowGeneratorDialogComponent } from '../loadflow/dialogs/loadflow-generator-dialog/loadflow-generator-dialog.component';

@Injectable({
    providedIn: 'root'
})

export class DialogService {

    constructor(private dialog: MatDialog, private dataClientService:DataClientService, private mapDataService: MapDataService ) {

    }

    private defaultOptions:MatDialogConfig<any> = { position: { top: "100px"}, disableClose: true }

    showClassificationToolDialog() {
        let dialogRef = this.dialog.open(ClassificationToolDialogComponent, this.defaultOptions)
        dialogRef.afterClosed().subscribe(input => {
            if ( input!==undefined && this.mapDataService.geographicalArea!==undefined) {
                let gaId = this.mapDataService.geographicalArea.id
                this.dataClientService.RunClassificationToolAll(gaId,input);
            }
        });
    }

    showDistSubstationEditorDialog() {
        let dialogRef = this.dialog.open(DistSubstationDialogComponent, this.defaultOptions)
    }

    showAboutDialog() {
        let dialogRef = this.dialog.open(AboutDialogComponent, this.defaultOptions)
    }

    showAboutLoadflowDialog() {
        let dialogRef = this.dialog.open(AboutLoadflowDialogComponent, this.defaultOptions)
    }

    showHelpLoadflowDialog() {
        let dialogRef = this.dialog.open(LoadflowHelpDialogComponent, this.defaultOptions)
    }

    showRegisterUserDialog() {
        let dialogRef = this.dialog.open(RegisterUserComponent, this.defaultOptions)
    }

    showLogonDialog() {
        let dialogRef = this.dialog.open(LogOnComponent, this.defaultOptions)
    }

    showChangePasswordDialog() {
        let dialogRef = this.dialog.open(ChangePasswordComponent, this.defaultOptions)
    }

    showResetPasswordDialog(token:string) {
        let options = { ... this.defaultOptions}
        options.data = { token: token}
        let dialogRef = this.dialog.open(ResetPasswordComponent, options)
    }

    showDatasetDialog(dataset: Dataset | null, parent: Dataset | null, onOk: (datasetId: number)=>void) {
        let options = Object.assign({},this.defaultOptions)
        if ( dataset ) {
            // make copy so we can freely edit it
            options.data = { dataset: Object.assign({},dataset), parent: parent}
        } else {
            options.data = { dataset: null, parent: parent}
        }
        let dialogRef = this.dialog.open(DatasetDialogComponent, options)
        dialogRef.afterClosed().subscribe((datasetId)=>{
            onOk(datasetId);
        });
    }

    showMessageDialog(data: MessageDialog | null, onOk?: ()=>void ) {
        let options = Object.assign({},this.defaultOptions)
        options.data = data
        let dialogRef = this.dialog.open(MessageDialogComponent, options)
        dialogRef.afterClosed().subscribe((input)=>{
            if ( input && onOk ) {
                onOk()
            }
        });
    }

    showNeedsLogonDialog() {
        let dialogRef = this.dialog.open(NeedsLogonComponent, this.defaultOptions)
    }

    showAboutElsiDialog() {
        let dialogRef = this.dialog.open(AboutElsiDialogComponent, this.defaultOptions)
    }

    showElsiHelpDialog() {
        let dialogRef = this.dialog.open(ElsiHelpDialogComponent, this.defaultOptions)
    }

    showLoadflowNodeDialog(cellObj?: ICellEditorDataDict) {
        let options = Object.assign({},this.defaultOptions)
        options.data = cellObj
        this.dialog.open(LoadflowNodeDialogComponent, options)
    }

    showLoadflowZoneDialog(cellObj?: ICellEditorDataDict, onClose?: (obj?: Zone) => void) {
        let options = Object.assign({},this.defaultOptions)
        options.data = cellObj
        let dialogRef = this.dialog.open(LoadflowZoneDialogComponent, options)
        dialogRef.afterClosed().subscribe((obj)=>{
            if ( onClose) {
                onClose(obj)
            }
        })
    }

    showLoadflowBoundaryDialog(cellObj?: ICellEditorDataDict) {
        let options = Object.assign({},this.defaultOptions)
        options.data = cellObj
        this.dialog.open(LoadflowBoundaryDialogComponent, options)
    }

    showLoadflowBranchDialog(branchEditorData?: IBranchEditorData, onClose?: (e: any)=>void ) {
        let options = Object.assign({},this.defaultOptions)
        options.data = branchEditorData
        let dialogRef = this.dialog.open(LoadflowBranchDialogComponent, options)
        dialogRef.afterClosed().subscribe((input)=>{
            if ( onClose ) {
                onClose(input)
            }
        });
    }

    showLoadflowCtrlDialog(cellObj?: ICellEditorDataDict) {
        let options = Object.assign({},this.defaultOptions)
        options.data = cellObj
        this.dialog.open(LoadflowCtrlDialogComponent, options)
    }

    showLoadflowLocationDialog(cellObj?: ICellEditorDataDict, onClose?: (e: any)=>void) {
        let options = Object.assign({},this.defaultOptions)
        options.data = cellObj
        let dialogRef = this.dialog.open(LoadflowLocationDialogComponent, options)
        dialogRef.afterClosed().subscribe((input)=>{
            if ( onClose ) {
                onClose(input)
            }
        });
    }

    showLoadflowTransportModelDialog(transportModel:TransportModel | undefined, onClose?: (e: any)=>void) {
        let options = Object.assign({},this.defaultOptions)
        options.data = transportModel
        let dialogRef = this.dialog.open(LoadflowTransportModelDialogComponent, options)
        dialogRef.afterClosed().subscribe((input)=>{
            if ( onClose ) {
                onClose(input)
            }
        });
    }

    showLoadflowGeneratorDialog(cellObj?: ICellEditorDataDict, onClose?: (obj?: Generator)=>void) {
        let options = Object.assign({},this.defaultOptions)
        options.data = cellObj
        let dialogRef = this.dialog.open(LoadflowGeneratorDialogComponent, options)
        dialogRef.afterClosed().subscribe((obj)=>{
            if ( onClose ) {
                onClose(obj)
            }
        });
    }
}
