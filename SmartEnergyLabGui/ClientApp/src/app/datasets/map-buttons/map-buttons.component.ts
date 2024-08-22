import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { DatasetsService } from '../datasets.service';
import { ICellEditorDataDict } from '../cell-editor/cell-editor.component';
import { IId } from 'src/app/data/app.data';

@Component({
    selector: 'app-map-buttons',
    templateUrl: './map-buttons.component.html',
    styleUrls: ['./map-buttons.component.css']
})
export class MapButtonsComponent implements OnInit {

    constructor(
        public datasetsService: DatasetsService
    ) { 
    }

    ngOnInit(): void {
    }

    @Input()
    element: any

    @Input()
    typeName: string = ''

    @Input()
    isDeleted: any

    @Output()
    onEdit: EventEmitter<any> = new EventEmitter<any>()

    @Output()
    onDelete: EventEmitter<IDeleteItem> = new EventEmitter<IDeleteItem>()

    edit() {
        // Check result count and ask user if necessary
        if ( this.datasetsService.isEditable && !this.isDeleted ) {
            this.datasetsService.canEdit( ()=>{
                this.onEdit.emit(this.element)
            })    
        }
    }

    delete() {
        if ( this.element && this.datasetsService.isEditable ) {
            if ( typeof this.element.id !== "number") {
                throw "unexpected type for id"
            }
            // see if we can delete the item
            let e = { element: this.element, canDelete: true }
            this.onDelete.emit(e);
            // if so go ahead and delete with further verification
            if ( e.canDelete) {
                let id:number = this.element.id
                this.datasetsService.deleteItemWithCheck(id,this.typeName)            
            }
        }
    }

    unDelete() {
        if ( this.element && this.datasetsService.isEditable  ) {
            if ( typeof this.element.id !== "number") {
                throw "unexpected type for id"
            }
            let id:number = this.element.id
            this.datasetsService.unDeleteItemWithCheck(id,this.typeName)    
        }
    }

    get isLocalEdit():boolean {
        return this.element?.datasetId === this.datasetsService.currentDataset?.id
    }

}

export interface IDeleteItem {
    element: any,
    canDelete: boolean
}
