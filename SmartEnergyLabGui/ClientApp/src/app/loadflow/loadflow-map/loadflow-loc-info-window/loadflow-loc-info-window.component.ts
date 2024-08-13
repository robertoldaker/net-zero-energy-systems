import { Component, EventEmitter, Output } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { LoadflowLocation, Node } from 'src/app/data/app.data';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { CellEditorData, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { IDeleteItem } from 'src/app/datasets/map-buttons/map-buttons.component';
import { DatasetsService, EditItemData, NewItemData } from 'src/app/datasets/datasets.service';

@Component({
  selector: 'app-loadflow-loc-info-window',
  templateUrl: './loadflow-loc-info-window.component.html',
  styleUrls: ['./loadflow-loc-info-window.component.css']
})

export class LoadflowLocInfoWindowComponent extends ComponentBase {
    constructor(private loadflowDataService: LoadflowDataService, private dialogService: DialogService, public datasetsService: DatasetsService) {
        super()
        this.addSub(this.loadflowDataService.ObjectSelected.subscribe( (selectedMapItem)=>{
            this.loc = selectedMapItem.location
        }))
    }

    @Output()
    onNewBranch: EventEmitter<any> = new EventEmitter();

    loc: LoadflowLocation| null = null
    get name():string {
        return this.loc ? this.loc.name : ''
    }

    get nodes(): Node[] {
        return this.loc ? this.loc.nodes : []
    }

    get reference():string {
        return this.loc ? this.loc.reference : ''
    }

    get isQB():boolean {
        return this.loc ? this.loc.isQB : false
    }

    get fillColor():string {
        return this.isQB ? 'grey' : '#7E4444'
    }

    edit(node: Node) {
        let itemData = new EditItemData<Node>(node, this.datasetsService)
        this.dialogService.showLoadflowNodeDialog(itemData)
    }

    delete( e: IDeleteItem) {
        let node: Node = e.element
        e.canDelete = this.loadflowDataService.canDeleteNode(node)
    }

    addNode(e: any) {
        let itemData = new NewItemData({code: this.loc?.reference})
        this.dialogService.showLoadflowNodeDialog(itemData);
    }

    addInternalBranch(e: any) {
        let itemData = new NewItemData({node1: this.loc?.reference, node2: this.loc?.reference})
        this.dialogService.showLoadflowBranchDialog(itemData);
    }

    addExternalBranch(e: any) {
        if ( this.onNewBranch ) {
            this.onNewBranch.emit(e)
        }
    }

    get canDeleteLocation():boolean {
        return this.loc?.nodes.length === 0
    }

    deleteLocation(e: any) {
        if ( this.loc ) {
            let id:number = this.loc.id
            this.datasetsService.deleteItemWithCheck(id,"GridSubstationLocation",()=>{})                
        }
    }
}

