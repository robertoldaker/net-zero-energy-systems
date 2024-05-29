import { Injectable } from "@angular/core";
import { DialogService } from "../dialogs/dialog.service";
import { DataClientService } from "../data/data-client.service";
import { ElsiDataService } from "../elsi/elsi-data.service";
import { LoadflowDataService } from "../loadflow/loadflow-data-service.service";
import { CellEditorData } from "../utils/cell-editor/cell-editor.component";
import { MessageDialogIcon } from "../dialogs/message-dialog/message-dialog.component";
import { DialogFooterButtonsEnum } from "../dialogs/dialog-footer/dialog-footer.component";
import { DatasetType } from "../data/app.data";

@Injectable({
    providedIn: 'root'
})

export class DatasetsService {

    constructor(
        private dataService:DataClientService, 
        private dialogService: DialogService, 
        private elsiDataService: ElsiDataService,
        private loadflowDataService: LoadflowDataService ) {

    }

    saveUserEditWithPrompt(value: string, cellData: CellEditorData, onEdited: (resp: string)=>void) {
        let userEdit = cellData.getUserEdit(value);
        this.dataService.GetDatasetResultCount(cellData.dataset.id,(count)=>{
            if ( count>0 ) {
                this.dialogService.showMessageDialog({
                    message: `This will delete <b>${count}</b> result(s) associated with the dataset <b>${cellData.dataset.name}</b>. Continue?`,
                    icon: MessageDialogIcon.Info,
                    buttons: DialogFooterButtonsEnum.OKCancel
                    }, ()=>{
                        this.dataService.SaveUserEdit(userEdit, (resp)=>{
                            this.afterEdit(cellData,resp, onEdited)
                        });
                    })        
                    
            } else {
                this.dataService.SaveUserEdit(userEdit,(resp)=>{
                    this.afterEdit(cellData,resp, onEdited)
                });
            }
        })
    }

    removeUserEditWithPrompt( cellData: CellEditorData, onEdited: (resp:string)=>void) {
        this.dataService.GetDatasetResultCount(cellData.dataset.id,(count)=>{
            if ( count>0 ) {
                this.dialogService.showMessageDialog({
                    message: `This will delete all results associated with the dataset [<b>${cellData.dataset.name}</b>]. Continue?`,
                    icon: MessageDialogIcon.Info,
                    buttons: DialogFooterButtonsEnum.OKCancel
                    }, ()=>{
                        if ( cellData.userEdit ) {
                            this.dataService.DeleteUserEdit(cellData.userEdit.id,(resp)=>{
                                this.afterEdit(cellData,resp, onEdited)
                            });
                        }
                    })        
            } else {                
                if ( cellData.userEdit ) {
                    this.dataService.DeleteUserEdit(cellData.userEdit.id,(resp)=>{
                        this.afterEdit(cellData, resp, onEdited)
                    });
                }
            }
        })    
    }

    afterEdit(cellData: CellEditorData, resp: string, onEdited: (resp:string)=>void ) {
        if ( cellData.dataset.type === DatasetType.Elsi ) {
            this.elsiDataService.loadDataset()
        } else if ( cellData.dataset.type === DatasetType.Loadflow ) {
            this.loadflowDataService.loadNetworkData(true)
        }
        if ( onEdited) {
            onEdited(resp)
        }
    }
}