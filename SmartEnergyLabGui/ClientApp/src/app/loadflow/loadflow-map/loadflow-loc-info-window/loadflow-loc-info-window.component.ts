import { Component, EventEmitter, Output, ViewChild } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { Branch, ILoadflowLocation, Node } from 'src/app/data/app.data';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { CellEditorData, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { IDeleteItem } from 'src/app/datasets/map-buttons/map-buttons.component';
import { DatasetsService, EditItemData, NewItemData } from 'src/app/datasets/datasets.service';
import { MatTabGroup } from '@angular/material/tabs';

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
            this.filterData();
            if ( this.matTabGroup) {
                this.matTabGroup.selectedIndex = 0
            }
        }))
        this.addSub(this.loadflowDataService.NetworkDataLoaded.subscribe( (networkData)=>{
            this.filterData();
        }))
    }

    @ViewChild(MatTabGroup)
    matTabGroup: MatTabGroup | undefined

    @Output()
    onNewBranch: EventEmitter<any> = new EventEmitter();

    loc: ILoadflowLocation| null = null
    private _nodes: Node[] = []
    private _deletedNodes: Node[] = []
    private _branchesInt: Branch[] = []
    private _deletedBranchesInt: Branch[] = []
    private _branchesExt: Branch[] = []
    private _deletedBranchesExt:Branch[] = []

    get name():string {
        return this.loc ? this.loc.name : ''
    }

    get nodes(): Node[] {
        return this._nodes
    }

    get deletedNodes(): Node[] {
        return this._deletedNodes
    }

    get branchesExt(): Branch[] {
        return this._branchesExt
    }

    get deletedBranchesExt(): Branch[] {
        return this._deletedBranchesExt
    }

    get branchesInt(): Branch[] {
        return this._branchesInt
    }

    get deletedBranchesInt(): Branch[] {
        return this._deletedBranchesInt
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

    editNode(node: Node) {
        let itemData = new EditItemData<Node>(node, this.datasetsService)
        this.dialogService.showLoadflowNodeDialog(itemData)
    }

    deleteNode( e: IDeleteItem) {
        let node: Node = e.element
        e.canDelete = this.loadflowDataService.canDeleteNode(node)
    }

    editBranch(branch: Branch) {
        let itemData = new EditItemData<Branch>(branch, this.datasetsService)
        this.dialogService.showLoadflowBranchDialog(itemData)
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
        return this._nodes.length == 0
    }

    deleteLocation(e: any) {
        if ( this.loc ) {
            let id:number = this.loc.id
            this.datasetsService.deleteItemWithCheck(id,"GridSubstationLocation")                
        }
    }

    filterData() {
        let nodes = this.loadflowDataService.networkData.nodes
        let branches = this.loadflowDataService.networkData.branches
        if ( this.loc ) {
            let locId = this.loc.id
            this._nodes = nodes.data.filter( m=>m.location && m.location.id === locId)
            this._deletedNodes = nodes.deletedData.filter( m=>m.location && m.location.id === locId)
            let bs = branches.data.filter( m=>m.node1LocationId>=0 && m.node2LocationId>=0 && m.node1LocationId===locId || m.node2LocationId == locId)
            this._branchesInt = bs.filter( m=>m.node1LocationId === m.node2LocationId)
            this._branchesExt = bs.filter( m=>m.node1LocationId !== m.node2LocationId)
            let deletedBs = branches.deletedData.filter( m=>m.node1LocationId>=0 && m.node2LocationId>=0 && m.node1LocationId===locId || m.node2LocationId == locId)
            this._deletedBranchesInt = deletedBs.filter( m=>m.node1LocationId === m.node2LocationId)
            this._deletedBranchesExt = deletedBs.filter( m=>m.node1LocationId !== m.node2LocationId)
        }
    }
}

