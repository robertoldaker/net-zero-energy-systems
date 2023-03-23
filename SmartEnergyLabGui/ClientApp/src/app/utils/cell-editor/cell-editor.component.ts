import { HtmlTagDefinition } from '@angular/compiler';
import { Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';
import { ElsiUserEdit, TableInfo } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { ElsiDataService } from 'src/app/elsi/elsi-data.service';

@Component({
    selector: 'app-cell-editor',
    templateUrl: './cell-editor.component.html',
    styleUrls: ['./cell-editor.component.css']
})
export class CellEditorComponent implements OnInit {

    constructor(private dataService: ElsiDataService) { 

    }

    ngOnInit(): void {

    }

    onFocus() {
        this.hasFocus = true
    }

    onBlur() {
        this.hasFocus = false;
        console.log('blur')
        if ( this.input ) {
            this.editValue = this.input.nativeElement.value
            this.input.nativeElement.value = this.value            
        }
    }

    mouseDown() {
        console.log(`mouseDown [${this.hasFocus}]`)
        this.hadFocus = this.hasFocus;
    }

    cancel(e: Event) {
        console.log(`cancel ${this.hadFocus}`)
        if ( !this.hadFocus) {
            return
        }
        e.stopPropagation()
        if ( this.input) {
            this.input.nativeElement.blur()
        }
    }

    @ViewChild('input')
    input: ElementRef | undefined;

    save(e: Event) {
        console.log(`save ${this.hadFocus}`)
        if ( !this.hadFocus) {
            return
        }
        e.stopPropagation()
        let value = this.editValue
        if ( value===this.value ) {
            console.log('ignoring same value')
            return;
        }
        let valueDouble = parseFloat(value)
        if ( this.scalingFac && !isNaN(valueDouble)) {
            value = (valueDouble / this.scalingFac).toString()
        }
        this.saveValue(value);
    }

    private saveValue(value: string) {
        let userEdit = this.data.getUserEdit(value)
        this.dataService.addUserEdit(userEdit);
    }

    saveSelect(e: Event) {
        if ( typeof this.data.value === 'string') {
            this.saveValue(this.data.value);    
        }
    }

    delete() {
        if ( this.data.userEdit) {
            this.dataService.removeUserEdit(this.data.userEdit.id);
        }
    }

    private editValue: string = ''
    hasFocus: boolean = false
    private hadFocus: boolean = false;

    @Input()
    data: CellEditorData = new CellEditorData()

    @Input()
    scalingFac: number | undefined

    @Input()
    decimalPlaces: number | undefined

    @Input()
    readOnly: boolean | undefined

    @Input()
    options: string[] | undefined

    get value():any {
        let value = this.data.value
        if ( typeof value == "number" ) {
            if ( this.scalingFac ) {
                value=value*this.scalingFac
            }
            if ( this.decimalPlaces!==undefined ) {
                value = value.toFixed(this.decimalPlaces)
            }
        } 
        return value
    }
}

export interface ICellEditorDataDict {
    [index: string]: CellEditorData
}

export class CellEditorData {
    constructor() {
        this.key = ''
        this.tableName = ''
        this.columnName = ''
        this.value = ''
        this.versionId = 0
    }
    key: string
    tableName: string
    columnName: string
    versionId: number
    value: number|string
    userEdit: ElsiUserEdit|undefined

    static GetCellDataObjects<T>(tableInfo: TableInfo<T>, keyFcn: (arg: T)=>string, versionId: number):ICellEditorDataDict[] {
        let cellData:ICellEditorDataDict[] = []
        let items = tableInfo.data;
        let columnNames = items.length>0 ? Object.getOwnPropertyNames(items[0]) : [];
        items.forEach( item=>{
            let data:any = item
            let cellObj:any = {}
            columnNames.forEach(col=>{
                let cd = new CellEditorData()
                cd.key = keyFcn(item)
                cd.columnName = col
                cd.tableName = tableInfo.tableName
                cd.value = data[col]
                cd.versionId = versionId
                cellObj[col] = cd
                // Find existing userEdit
                cd.userEdit = tableInfo.userEdits.find(m=>m.columnName==cd.columnName && m.key==cd.key)
            })
            cellData.push(cellObj)
        })
        return cellData
    }

    getUserEdit(value: string):ElsiUserEdit {
        let userEdit = {
            id: (this.userEdit) ? this.userEdit.id : 0,
            key: this.key,
            tableName: this.tableName,
            columnName: this.columnName,
            value: value,
            versionId: this.versionId 
        }
        return userEdit
    }
}
