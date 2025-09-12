import { Component, Injectable } from '@angular/core';
import { MatDialog, MatDialogConfig } from '@angular/material/dialog';
import { AboutDialogComponent } from '../main/about-dialog/about-dialog.component';
import { AboutBoundCalcDialogComponent } from '../boundcalc/about-boundcalc-dialog/about-boundcalc-dialog.component';
import { ClassificationToolDialogComponent } from '../classification/classification-tool-dialog/classification-tool-dialog.component';
import { DistSubstationDialogComponent } from '../low-voltage/dist-substation-dialog/dist-substation-dialog.component';
import { BoundCalcHelpDialogComponent } from '../boundcalc/boundcalc-help-dialog/boundcalc-help-dialog.component';
import { LogOnComponent } from '../users/log-on/log-on.component';
import { RegisterUserComponent } from '../users/register-user/register-user.component';
import { DataClientService } from '../data/data-client.service';
import { MapDataService } from '../low-voltage/map-data.service';
import { ChangePasswordComponent } from '../users/change-password/change-password.component';
import { Dataset, Node, GenerationModel, Zone, Generator } from '../data/app.data';
import { MessageDialog, MessageDialogComponent } from './message-dialog/message-dialog.component';
import { AboutElsiDialogComponent } from '../elsi/about-elsi-dialog/about-elsi-dialog.component';
import { ElsiHelpDialogComponent } from '../elsi/elsi-help-dialog/elsi-help-dialog.component';
import { NeedsLogonComponent } from '../main/main-menu/needs-logon/needs-logon.component';
import { ResetPasswordComponent } from '../users/reset-password/reset-password.component';
import { DatasetDialogComponent } from '../datasets/dataset-dialog/dataset-dialog.component';
import { BoundCalcNodeDialogComponent } from '../boundcalc/dialogs/boundcalc-node-dialog/boundcalc-node-dialog.component';
import { ICellEditorDataDict } from '../datasets/cell-editor/cell-editor.component';
import { BoundCalcZoneDialogComponent } from '../boundcalc/dialogs/boundcalc-zone-dialog/boundcalc-zone-dialog.component';
import { BoundCalcBoundaryDialogComponent } from '../boundcalc/dialogs/boundcalc-boundary-dialog/boundcalc-boundary-dialog.component';
import { BoundCalcBranchDialogComponent } from '../boundcalc/dialogs/boundcalc-branch-dialog/boundcalc-branch-dialog.component';
import { BoundCalcCtrlDialogComponent } from '../boundcalc/dialogs/boundcalc-ctrl-dialog/boundcalc-ctrl-dialog.component';
import { BoundCalcLocationDialogComponent } from '../boundcalc/dialogs/boundcalc-location-dialog/boundcalc-location-dialog.component';
import { IBranchEditorData } from '../boundcalc/boundcalc-data-service.service';
import { BoundCalcGenerationModelDialogComponent } from '../boundcalc/dialogs/boundcalc-generation-model-dialog/boundcalc-generation-model-dialog.component';
import { BoundCalcGeneratorDialogComponent } from '../boundcalc/dialogs/boundcalc-generator-dialog/boundcalc-generator-dialog.component';

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

    showAboutBoundCalcDialog() {
        let dialogRef = this.dialog.open(AboutBoundCalcDialogComponent, this.defaultOptions)
    }

    showHelpBoundCalcDialog() {
        let dialogRef = this.dialog.open(BoundCalcHelpDialogComponent, this.defaultOptions)
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

    showBoundCalcNodeDialog(cellObj?: ICellEditorDataDict) {
        let options = Object.assign({},this.defaultOptions)
        options.data = cellObj
        this.dialog.open(BoundCalcNodeDialogComponent, options)
    }

    showBoundCalcZoneDialog(cellObj?: ICellEditorDataDict, onClose?: (obj?: Zone) => void) {
        let options = Object.assign({},this.defaultOptions)
        options.data = cellObj
        let dialogRef = this.dialog.open(BoundCalcZoneDialogComponent, options)
        dialogRef.afterClosed().subscribe((obj)=>{
            if ( onClose) {
                onClose(obj)
            }
        })
    }

    showBoundCalcBoundaryDialog(cellObj?: ICellEditorDataDict) {
        let options = Object.assign({},this.defaultOptions)
        options.data = cellObj
        this.dialog.open(BoundCalcBoundaryDialogComponent, options)
    }

    showBoundCalcBranchDialog(branchEditorData?: IBranchEditorData, onClose?: (e: any)=>void ) {
        let options = Object.assign({},this.defaultOptions)
        options.data = branchEditorData
        let dialogRef = this.dialog.open(BoundCalcBranchDialogComponent, options)
        dialogRef.afterClosed().subscribe((input)=>{
            if ( onClose ) {
                onClose(input)
            }
        });
    }

    showBoundCalcCtrlDialog(cellObj?: ICellEditorDataDict) {
        let options = Object.assign({},this.defaultOptions)
        options.data = cellObj
        this.dialog.open(BoundCalcCtrlDialogComponent, options)
    }

    showBoundCalcLocationDialog(cellObj?: ICellEditorDataDict, onClose?: (e: any)=>void) {
        let options = Object.assign({},this.defaultOptions)
        options.data = cellObj
        let dialogRef = this.dialog.open(BoundCalcLocationDialogComponent, options)
        dialogRef.afterClosed().subscribe((input)=>{
            if ( onClose ) {
                onClose(input)
            }
        });
    }

    showBoundCalcGenerationModelDialog(generationModel:GenerationModel | undefined, onClose?: (e: any)=>void) {
        let options = Object.assign({},this.defaultOptions)
        options.data = generationModel
        let dialogRef = this.dialog.open(BoundCalcGenerationModelDialogComponent, options)
        dialogRef.afterClosed().subscribe((input)=>{
            if ( onClose ) {
                onClose(input)
            }
        });
    }

    showBoundCalcGeneratorDialog(cellObj?: ICellEditorDataDict, onClose?: (obj?: Generator)=>void) {
        let options = Object.assign({},this.defaultOptions)
        options.data = cellObj
        let dialogRef = this.dialog.open(BoundCalcGeneratorDialogComponent, options)
        dialogRef.afterClosed().subscribe((obj)=>{
            if ( onClose ) {
                onClose(obj)
            }
        });
    }
}
