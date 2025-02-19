import { Injectable } from "@angular/core";
import { DialogService } from "../dialogs/dialog.service";
import { DataClientService } from "../data/data-client.service";
import { ElsiDataService } from "../elsi/elsi-data.service";
import { LoadflowDataService } from "../loadflow/loadflow-data-service.service";
import { CellEditorData, ICellEditorDataDict } from "./cell-editor/cell-editor.component";
import { MessageDialog, MessageDialogIcon } from "../dialogs/message-dialog/message-dialog.component";
import { DialogFooterButtonsEnum } from "../dialogs/dialog-footer/dialog-footer.component";
import { Dataset, DatasetData, DatasetType } from "../data/app.data";
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

    saveUserEditWithPrompt(value: string, cellData: CellEditorData, onEdited: (resp: DatasetData<any>)=>void, onError: (resp: any)=>void) {
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

    saveUserEdit(value: string, cellData: CellEditorData, onEdited: (resp: DatasetData<any>)=>void, onError: (resp: any) =>void) {
        if ( this.currentDataset ) {
            let id = parseInt(cellData.key)
            let data:IFormControlDict = {}
            data[cellData.columnName] = value
            let editItemData = {id: id, datasetId: this.currentDataset?.id, className: cellData.tableName, data: data }
            this.dataService.EditItem(editItemData, (resp)=>{
                this.afterEdit(cellData,resp, onEdited)
            }, (errors)=>{
                onError(errors)
            })    
        }
    }

    removeUserEditWithPrompt( cellData: CellEditorData, onEdited: (resp:DatasetData<any>)=>void) {
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

    afterEdit(cellData: CellEditorData, resp: any, onEdited: (resp:DatasetData<any>)=>void ) {
        if ( this.currentDataset?.type === DatasetType.Elsi ) {
            this.elsiDataService.loadDataset()
        } else if ( this.currentDataset?.type === DatasetType.BoundCalc ) {
            this.loadflowDataService.afterEdit(resp)
        }    
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

    deleteItemWithCheck(id: number, className: string) {
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
                        this.deleteItem(id, className, dataset)
                    })                            
            } else {
                this.deleteItem(id, className, dataset)
            }
        })    
    }

    private deleteItem(id: number, className: string, dataset: Dataset) {
        this.dataService.DeleteItem({id: id, className: className, datasetId: dataset.id}, (resp)=>{
            // this means it couldn't be delete
            if ( resp.msg ) {
                this.dialogService.showMessageDialog(new MessageDialog(resp.msg))
            } else {
                this.afterDeleteItem(id, className, dataset)
            }
        });
    }

    private afterDeleteItem(id: number, className: string, dataset: Dataset) {
        if ( dataset.type == DatasetType.BoundCalc) {
            this.loadflowDataService.afterDelete(id, className, dataset)
        }
    }

    unDeleteItemWithCheck(id: number, className: string) {
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
                        this.unDeleteItem(id, className, dataset)
                    })                            
            } else {
                this.unDeleteItem(id, className, dataset)
            }
        })    
    }    

    private unDeleteItem(id: number, className: string, dataset: Dataset) {
        this.dataService.UnDeleteItem({id: id, className: className, datasetId: dataset.id}, (resp)=>{
            // this means it couldn't be unDeleted
            if ( resp.msg ) {
                this.dialogService.showMessageDialog(new MessageDialog(resp.msg))
            } else {
                this.afterUnDeleteItem(resp.datasets)
            }
        });
    }

    private afterUnDeleteItem(datasets: DatasetData<any>[]) {
        if ( this.currentDataset?.type === DatasetType.Elsi ) {
            this.elsiDataService.loadDataset()
        } else if ( this.currentDataset?.type === DatasetType.BoundCalc ) {
            this.loadflowDataService.afterUnDelete(datasets)
        }  
    }

    public static deleteDatasetData(dd: DatasetData<any>, id: number, dataset: Dataset) {
        let index = dd.data.findIndex(m=>m.id == id)
        // delete from list of data
        if (index>=0) {
            let d = dd.data[index];
            dd.data.splice(index,1)
            // non-source edit so add to list of delete data
            if ( d.datasetId != dataset.id) {
                dd.deletedData.push(d)
            }
        }
    }

    public static updateDatasetData(dd: DatasetData<any>, resp: DatasetData<any>) {
        for( let d of resp.data) {
            // add or replace in data
            let index = dd.data.findIndex(m=>m.id == d.id)
            if ( index>=0 ) {
                dd.data[index] = d;
            } else {
                dd.data.push(d);
            }
            // remove from deleted data
            index = dd.deletedData.findIndex(m=>m.id == d.id)
            if ( index>=0 ) {
                dd.deletedData.splice(index,1)
            }
            let ues = dd.userEdits.filter(m=>m.key == d.id)
            for (let ue of ues) {
                let index = dd.userEdits.findIndex(m=>m.id === ue.id)
                dd.userEdits.splice(index,1)
            }                
        }
        for( let d of resp.deletedData) {
            // add or replace in deletedData
            let index = dd.deletedData.findIndex(m=>m.id == d.id)
            if ( index>=0 ) {
                dd.deletedData[index] = d;
            } else {
                dd.deletedData.push(d);
            }
            // remove from data if it exists
            index = dd.data.findIndex(m=>m.id == d.id)
            if ( index>=0 ) {
                dd.data.splice(index,1)
            }
        }
        for( let ue of resp.userEdits ) {
            dd.userEdits.push(ue);
        }
    }

}

export interface IDatasetId {
    datasetId: number
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
