import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { DatasetsService } from '../datasets.service';
import { ICellEditorDataDict } from '../cell-editor/cell-editor.component';

@Component({
    selector: 'app-cell-buttons',
    templateUrl: './cell-buttons.component.html',
    styleUrls: ['./cell-buttons.component.css']
})
export class CellButtonsComponent implements OnInit {

    constructor(
        public datasetsService: DatasetsService
    ) { 
    }

    ngOnInit(): void {
    }

    @Input()
    element: ICellEditorDataDict | undefined

    @Input()
    typeName: string = ''

    @Output()
    onEdit: EventEmitter<any> = new EventEmitter<any>()

    @Output()
    onDelete: EventEmitter<IDeleteItem> = new EventEmitter<IDeleteItem>()

    edit() {
        // Check result count and ask user if necessary
        this.datasetsService.canEdit( ()=>{
            this.onEdit.emit(this.element)
        })
    }

    delete() {
        if ( this.element ) {
            if ( typeof this.element.id.value !== "number") {
                throw "unexpected type for id"
            }
            // see if we can delete the item
            let e = { element: this.element, canDelete: true }
            this.onDelete.emit(e);
            // if so go ahead and delete with further verification
            if ( e.canDelete) {
                let id:number = this.element.id.value
                this.datasetsService.deleteItemWithCheck(id,this.typeName)            
            }
        }
    }

    unDelete() {
        if ( this.element ) {
            if ( typeof this.element.id.value !== "number") {
                throw "unexpected type for id"
            }
            let id:number = this.element.id.value
            this.datasetsService.unDeleteItemWithCheck(id,this.typeName)    
        }
    }

    get isLocalEdit():boolean {
        return this.element && this.element._isLocalEdit
    }

}

export interface IDeleteItem {
    element: ICellEditorDataDict,
    canDelete: boolean
}
