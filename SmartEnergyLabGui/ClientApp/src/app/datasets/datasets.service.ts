import { Injectable } from "@angular/core";
import { DialogService } from "../dialogs/dialog.service";
import { DataClientService } from "../data/data-client.service";
import { ElsiDataService } from "../elsi/elsi-data.service";
import { LoadflowDataService } from "../loadflow/loadflow-data-service.service";
import { CellEditorData, ICellEditorDataDict } from "./cell-editor/cell-editor.component";
import { MessageDialog, MessageDialogIcon } from "../dialogs/message-dialog/message-dialog.component";
import { DialogFooterButtonsEnum } from "../dialogs/dialog-footer/dialog-footer.component";
import { Dataset, DatasetType } from "../data/app.data";
import { UserService } from "../users/user.service";
import { IFormControlDict } from "../dialogs/dialog-base";

@Injectable({
    providedIn: 'root'
})

export class DatasetsService {

    constructor(
        private dataService:DataClientService, 
        private dialogService: DialogService, 
        private elsiDataService: ElsiDataService,
        private loadflowDataService: LoadflowDataService,
        private userService: UserService ) {
        
    }

    currentDataset: Dataset | undefined
    setDataset(dataset: Dataset | undefined) {
        this.currentDataset = dataset;
    }
    get isEditable():boolean {
        if ( this.currentDataset) {
            if ( !this.currentDataset.isReadOnly || this.userService.isAdmin ) {
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }

    saveUserEditWithPrompt(value: string, cellData: CellEditorData, onEdited: (resp: string)=>void, onError: (resp: any)=>void) {
        this.dataService.GetDatasetResultCount(cellData.dataset.id,(count)=>{
            if ( count>0 ) {
                this.dialogService.showMessageDialog({
                    message: `This will delete <b>${count}</b> result(s) associated with the dataset <b>${cellData.dataset.name}</b>. Continue?`,
                    icon: MessageDialogIcon.Info,
                    buttons: DialogFooterButtonsEnum.OKCancel
                    }, ()=>{
                        this.saveUserEdit(value,cellData,onEdited,onError)
                    })        
                    
            } else {
                this.saveUserEdit(value,cellData,onEdited,onError)
            }
        })
    }

    saveUserEdit(value: string, cellData: CellEditorData, onEdited: (resp: string)=>void, onError: (resp: any) =>void) {
        if ( this.currentDataset ) {
            let id = parseInt(cellData.key)
            let data:IFormControlDict = {}
            data[cellData.columnName] = value
            this.dataService.EditItem({id: id, datasetId: this.currentDataset?.id, className: cellData.tableName, data: data }, (resp)=>{
                this.afterEdit(cellData,resp, onEdited)
            }, (errors)=>{
                onError(errors)
            })    
        }
    }

    removeUserEditWithPrompt( cellData: CellEditorData, onEdited: (resp:string)=>void) {
        this.dataService.GetDatasetResultCount(cellData.dataset.id,(count)=>{
            if ( count>0 ) {
                this.dialogService.showMessageDialog({
                    message: `This will delete <b>${count}</b> result(s) associated with the dataset <b>${cellData.dataset.name}</b>. Continue?`,
                    icon: MessageDialogIcon.Info,
                    buttons: DialogFooterButtonsEnum.OKCancel
                    }, ()=>{
                        if ( cellData.userEdit ) {
                            this.saveUserEdit(cellData.userEdit.prevValue,cellData,onEdited, ()=>{})
                        }
                    })                            
            } else {
                if ( cellData.userEdit ) {
                    this.saveUserEdit(cellData.userEdit.prevValue,cellData,onEdited, ()=>{})
                }
            }
        })
    }

    afterEdit(cellData: CellEditorData, resp: string, onEdited: (resp:string)=>void ) {
        this.refreshData()
        if ( onEdited) {
            onEdited(resp)
        }
    }

    canEdit( onEdit: ()=>void) {
        if ( !this.currentDataset ) {
            throw "No dataset defined";
        }
        let dataset = this.currentDataset;
        this.dataService.GetDatasetResultCount(dataset.id,(count)=>{
            if ( count>0 ) {
                this.dialogService.showMessageDialog({
                    message: `Editing this item will delete <b>${count}</b> result(s) associated with the dataset <b>${dataset.name}</b>. Continue?`,
                    icon: MessageDialogIcon.Info,
                    buttons: DialogFooterButtonsEnum.OKCancel
                    }, ()=>{
                        onEdit()
                    })                            
            } else {
                onEdit();
            }
        })    
    }

    canAdd( onEdit: ()=>void) {
        if ( !this.currentDataset ) {
            throw "No dataset defined";
        }
        let dataset = this.currentDataset;
        this.dataService.GetDatasetResultCount(dataset.id,(count)=>{
            if ( count>0 ) {
                this.dialogService.showMessageDialog({
                    message: `Adding this item will delete <b>${count}</b> result(s) associated with the dataset <b>${dataset.name}</b>. Continue?`,
                    icon: MessageDialogIcon.Info,
                    buttons: DialogFooterButtonsEnum.OKCancel
                    }, ()=>{
                        onEdit()
                    })                            
            } else {
                onEdit();
            }
        })    
    }

    deleteItemWithCheck(id: number, className: string, onDeleted: ()=>void) {
        if ( !this.currentDataset ) {
            throw "No dataset defined";
        }
        let dataset = this.currentDataset;
        this.dataService.GetDatasetResultCount(dataset.id,(count)=>{
            if ( count>0 ) {
                this.dialogService.showMessageDialog({
                    message: `This will delete <b>${count}</b> result(s) associated with the dataset <b>${dataset.name}</b>. Continue?`,
                    icon: MessageDialogIcon.Info,
                    buttons: DialogFooterButtonsEnum.OKCancel
                    }, ()=>{
                        this.deleteItem(id, className, dataset, onDeleted)
                    })                            
            } else {
                this.deleteItem(id, className, dataset, onDeleted)
            }
        })    
    }

    private deleteItem(id: number, className: string, dataset: Dataset, onDeleted: ()=>void) {
        this.dataService.DeleteItem({id: id, className: className, datasetId: dataset.id}, (resp)=>{
            this.refreshData();
            // this means it couldn't be unDeleted
            if ( resp.msg ) {
                this.dialogService.showMessageDialog(new MessageDialog(resp.msg))
            } else {
                onDeleted()
            }
            onDeleted();
        });
    }

    unDeleteItemWithCheck(id: number, className: string, onDeleted: ()=>void) {
        if ( !this.currentDataset ) {
            throw "No dataset defined";
        }
        let dataset = this.currentDataset;
        this.dataService.GetDatasetResultCount(dataset.id,(count)=>{
            if ( count>0 ) {
                this.dialogService.showMessageDialog({
                    message: `This will delete <b>${count}</b> result(s) associated with the dataset <b>${dataset.name}</b>. Continue?`,
                    icon: MessageDialogIcon.Info,
                    buttons: DialogFooterButtonsEnum.OKCancel
                    }, ()=>{
                        this.unDeleteItem(id, className, dataset, onDeleted)
                    })                            
            } else {
                this.unDeleteItem(id, className, dataset, onDeleted)
            }
        })    
    }    

    private unDeleteItem(id: number, className: string, dataset: Dataset, onDeleted: ()=>void) {
        this.dataService.UnDeleteItem({id: id, className: className, datasetId: dataset.id}, (resp)=>{
            this.refreshData();
            // this means it couldn't be unDeleted
            if ( resp.msg ) {
                this.dialogService.showMessageDialog(new MessageDialog(resp.msg))
            } else {
                onDeleted()
            }
        });
    }

    public refreshData() {
        if ( this.currentDataset ) {
            let dataset = this.currentDataset
            if ( dataset.type === DatasetType.Elsi ) {
                this.elsiDataService.loadDataset()
            } else if ( dataset.type === DatasetType.Loadflow ) {
                this.loadflowDataService.reload()
            }    
        }
    }
}

export interface IDatasetId {
    datasetId: number
}

export class EditItemData<T extends IDatasetId> implements ICellEditorDataDict {
    constructor(item: T, datasetsService: DatasetsService, isDeleted?:boolean) {
        this._data = item
        this._isDeleted = isDeleted
        this._isLocalDataset = item.datasetId === datasetsService.currentDataset?.id
        this._isLocalEdit = isDeleted || this._isLocalEdit
    }
    [index: string]: CellEditorData
    _data: any
    _isDeleted: any
    _isLocalDataset: any
    _isLocalEdit: any 
}

export class NewItemData<T> implements ICellEditorDataDict {
    constructor(item: any) {
        this._data = item
        this._isDeleted = false
        this._isLocalDataset = true
        this._isLocalEdit = true
    }
    [index: string]: CellEditorData
    _data: any
    _isDeleted: any
    _isLocalDataset: any
    _isLocalEdit: any 
}
