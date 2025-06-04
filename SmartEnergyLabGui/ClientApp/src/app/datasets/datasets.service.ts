import { EventEmitter, Injectable } from "@angular/core";
import { DialogService } from "../dialogs/dialog.service";
import { DataClientService } from "../data/data-client.service";
import { ElsiDataService } from "../elsi/elsi-data.service";
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
        private userService: UserService ) {

    }

    currentDataset: Dataset | undefined
    setDataset(dataset: Dataset | undefined) {
        this.currentDataset = dataset;
    }
    customData: {name: string, value: any} | undefined
    //
    /*setEditFcns(thisObj: any,
        afterEditFcn: (datasets: DatasetData<any>[])=>void,
        afterDeleteFcn: (id: number, className: string, dataset: Dataset)=>void ,
        afterUnDeleteFcn: (datasets: DatasetData<any>[])=>void ) {
        this.editFcns={thisObj: thisObj,
            afterEditFcn: afterEditFcn,
            afterDeleteFcn: afterDeleteFcn,
            afterUnDeleteFcn: afterUnDeleteFcn }
    }
    editFcns: {thisObj: any,
        afterEditFcn: (datasets: DatasetData<any>[]) => void,
        afterDeleteFcn: (id: number, className: string, dataset: Dataset) =>void,
        afterUnDeleteFcn: (datasets: DatasetData<any>[]) => void
        } | undefined
    */

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
            if ( this.customData) {
                data[this.customData.name] = this.customData.value
            }
            let editItemData = {id: id, datasetId: this.currentDataset.id, className: cellData.tableName, data: data }
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
                    this.saveUserEdit(cellData.userEdit.prevValue,cellData,onEdited, (resp)=>{
                        console.log('error',resp)
                    })
                }
            }
        })
    }

    afterEdit(cellData: CellEditorData, resp: any, onEdited: (resp:DatasetData<any>)=>void ) {
        if ( this.currentDataset ) {
            let type = this.currentDataset.type
            if ( type === DatasetType.Elsi ) {
                this.elsiDataService.loadDataset()
            } else {
                //??this.editFcns.afterEditFcn.call(this.editFcns.thisObj,resp)
                this.AfterEdit.emit({ type: type, datasets: resp})
            }
            if ( onEdited) {
                onEdited(resp)
            }
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
        let itemData = { id: id, className: className, dataset: dataset }
        this.dataService.GetDatasetResultCount(dataset.id,(count)=>{
            if ( count>0 ) {
                this.dialogService.showMessageDialog({
                    message: `This will delete <b>${count}</b> result(s) associated with the dataset <b>${dataset.name}</b>. Continue?`,
                    icon: MessageDialogIcon.Info,
                    buttons: DialogFooterButtonsEnum.OKCancel
                    }, ()=>{
                        this.deleteItem(itemData)
                    })
            } else {
                this.deleteItem(itemData)
            }
        })
    }

    private deleteItem(itemData: DeleteItemData) {
        let data:IFormControlDict  = {}
        if ( this.customData) {
            data[this.customData.name] = this.customData.value
        }
        this.dataService.DeleteItem({id: itemData.id, className: itemData.className, datasetId: itemData.dataset.id, data: data}, (resp)=>{
            // this means it couldn't be deleted
            if ( resp.msg ) {
                this.dialogService.showMessageDialog(new MessageDialog(resp.msg))
            } else {
                this.AfterDelete.emit({type: itemData.dataset.type, deletedItems: resp.deletedItems, datasets: resp.datasets})
            }
        });
    }

    unDeleteItemWithCheck(id: number, className: string) {
        if ( !this.currentDataset ) {
            throw "No dataset defined";
        }
        let dataset = this.currentDataset
        let deleteItem = {id: id, className: className, dataset: dataset}
        this.dataService.GetDatasetResultCount(dataset.id,(count)=>{
            if ( count>0 ) {
                this.dialogService.showMessageDialog({
                    message: `This will delete <b>${count}</b> result(s) associated with the dataset <b>${dataset.name}</b>. Continue?`,
                    icon: MessageDialogIcon.Info,
                    buttons: DialogFooterButtonsEnum.OKCancel
                    }, ()=>{
                        this.unDeleteItem(deleteItem)
                    })
            } else {
                this.unDeleteItem(deleteItem)
            }
        })
    }

    private unDeleteItem(deleteItem: DeleteItemData) {
        let data:IFormControlDict  = {}
        if ( this.customData) {
            data[this.customData.name] = this.customData.value
        }
        this.dataService.UnDeleteItem({id: deleteItem.id, className: deleteItem.className, datasetId: deleteItem.dataset.id, data: data}, (resp)=>{
            // this means it couldn't be unDeleted
            if ( resp.msg ) {
                this.dialogService.showMessageDialog(new MessageDialog(resp.msg))
            } else {
                this.AfterUnDelete.emit({ type: deleteItem.dataset.type, deleteItem: deleteItem, datasets: resp.datasets })
            }
        });
    }

    public static deleteSourceData(dd: DatasetData<any>, id: number) {
        let index = dd.data.findIndex(m=>m.id == id)
        // delete from list of data
        if (index>=0) {
            let d = dd.data[index];
            dd.data.splice(index,1)
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
            // Remove any user edits referencing this data as these will be added later
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
        // push new user edits
        for( let ue of resp.userEdits ) {
            dd.userEdits.push(ue);
        }
    }

    AfterEdit:EventEmitter<AfterEditData> = new EventEmitter<AfterEditData>()
    AfterDelete:EventEmitter<AfterDeleteData> = new EventEmitter<AfterDeleteData>()
    AfterUnDelete:EventEmitter<AfterUnDeleteData> = new EventEmitter<AfterUnDeleteData>()

}
export interface AfterEditData {
    type: DatasetType
    datasets: DatasetData<any>[]
}

export interface AfterDeleteData {
    type: DatasetType
    deletedItems: DeleteItemData[]
    datasets: DatasetData<any>[]
}

export interface AfterUnDeleteData {
    type: DatasetType
    deleteItem: DeleteItemData
    datasets: DatasetData<any>[]
}

export interface DeleteItemData {
    id: number,
    className: string,
    dataset: Dataset
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
