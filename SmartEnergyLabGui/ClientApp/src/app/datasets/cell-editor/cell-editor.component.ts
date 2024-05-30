import { AfterViewInit, Component, ElementRef, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { Dataset, DatasetData, DatasetType, UserEdit } from 'src/app/data/app.data';
import { DatasetsService } from 'src/app/datasets/datasets.service';

@Component({
    selector: 'app-cell-editor',
    templateUrl: './cell-editor.component.html',
    styleUrls: ['./cell-editor.component.css']
})
export class CellEditorComponent {

    constructor(private datasetsService: DatasetsService) { 

    }

    ngOnInit(): void {

    }

    onFocus() {
        this.hasFocus = true
    }

    onBlur() {
        this.hasFocus = false;
        if ( this.input ) {
            this.editValue = this.input.nativeElement.value
            this.input.nativeElement.value = this.value            
        }
    }

    mouseDown() {
        this.hadFocus = this.hasFocus;
    }

    cancel(e: Event) {
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
        if ( !this.hadFocus) {
            return
        }
        e.stopPropagation()
        let value = this.editValue
        if ( value===this.value ) {
            return;
        }
        let valueDouble = parseFloat(value)
        if ( this.scalingFac && !isNaN(valueDouble)) {
            value = (valueDouble / this.scalingFac).toString()
        }
        this.saveValue(value);
    }

    private saveValue(value: string) {
        if ( this.data ) {
            this.datasetsService.saveUserEditWithPrompt(value, this.data, (resp)=>{
                this.onEdited.emit(this.data)
            })    
        }
    }

    saveSelect(e: Event) {
        if ( typeof this.data.value === 'string') {
            this.saveValue(this.data.value);    
        }
    }

    delete() {
        if ( this.data ) {
            this.datasetsService.removeUserEditWithPrompt(this.data, (resp)=>{
                this.onEdited.emit(this.data)
            })        
        }
    }

    private editValue: string = ''
    hasFocus: boolean = false
    private hadFocus: boolean = false;

    @Input()
    data: CellEditorData = new CellEditorData({id: 0, name: '',parent: null, type: DatasetType.Elsi, isReadOnly: true})

    @Input()
    scalingFac: number | undefined

    @Input()
    decimalPlaces: number | undefined

    get readOnly(): boolean {
        return this.data.dataset.isReadOnly
    }

    @Input()
    options: string[] | undefined

    @Output()
    onEdited: EventEmitter<CellEditorData> = new EventEmitter<CellEditorData>()

    get value():any {
        let value = this.data?.value
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
    constructor(dataset: Dataset) {
        this.key = ''
        this.tableName = ''
        this.columnName = ''
        this.value = ''
        this.dataset = dataset;
    }
    key: string
    tableName: string
    columnName: string
    dataset: Dataset
    value: number|string
    userEdit: UserEdit|undefined

    getUserEdit(value: string):UserEdit {
        let userEdit = {
            id: (this.userEdit) ? this.userEdit.id : 0,
            key: this.key,
            tableName: this.tableName,
            columnName: this.columnName,
            value: value,
            newDatasetId: this.dataset.id
        }
        return userEdit
    }

}


export class DataFilter {
    constructor(take: number) 
    {
        this.take = take;
    }

    GetCellDataObjects<T>(dataset: Dataset, datasetData: DatasetData<T>, 
            keyFcn: (arg: T, col: string)=>string, 
            colFcn?: (col: string)=>string):ICellEditorDataDict[] {
        let cellData:ICellEditorDataDict[] = []
        let items = datasetData.data;
        let columnNames = items.length>0 ? Object.getOwnPropertyNames(items[0]) : [];
        if ( this.searchStr ) {
            items = this.filterData(items,columnNames)
        }
        if ( this.onlyEditedRows) {
            items = this.filterByEdited(items, columnNames, datasetData,keyFcn,colFcn)
        }
        if ( this.sort && this.sort.active) {
            this.sortData(items)
        }
        this.dataLength = items.length;
        let skip:number = this.skip
        let take:number = this.take
        for( let i=skip;i<items.length;i++) {
            let data:any = items[i]
            let cellObj:any = {}
            columnNames.forEach(col=>{
                let cd = new CellEditorData(dataset)
                cd.key = keyFcn(data,col)
                cd.columnName = colFcn ? colFcn(col) : col
                cd.tableName = datasetData.tableName
                cd.value = data[col]
                cd.dataset = dataset
                cellObj[col] = cd
                // Find existing userEdit
                cd.userEdit = datasetData.userEdits.find(m=>m.columnName==cd.columnName && m.key==cd.key)
            })
            cellData.push(cellObj)
            if ( take && cellData.length>=take) {
                break;
            }
        }
        return cellData
    }

    private filterData(items: any[], columnNames: string[]):any[] {
        let lcSearchStr = this.searchStr.toLowerCase()
        items = items.filter(item=>{
            let filter = false;
            columnNames.forEach(col=>{
                let data = item[col];
                let dataStr = ''
                if ( typeof(data) === 'string') {
                    dataStr = data.toLowerCase();
                } else if (typeof(data) === 'number') {
                    dataStr = data.toString().toLowerCase()
                } 
                if ( dataStr && dataStr.includes(lcSearchStr) ) {
                    filter = true;
                }
            })
            return filter
        })
        return items;
    }

    private filterByEdited<T>(items: any[], 
        columnNames: string[],
        datasetData: DatasetData<T>,
        keyFcn: (arg: T, col: string)=>string,
        colFcn?: (col: string)=>string
        ):any[] {
        items = items.filter(item=>{
            let filter = false;
            columnNames.forEach(col=>{
                let key = keyFcn(item, col)
                let colName = colFcn ? colFcn(col) : col
                if ( datasetData.userEdits.find(m=>m.columnName==colName && m.key==key) !== undefined) {
                    filter = true
                }
            })
            return filter
        })
        return items;
    }

    private sortData(items: any[]):any[] {
        items.sort((item1,item2)=>{
            let result = 0;
            if ( this.sort ) {
                let sort = this.sort
                let data1 = item1[sort.active]
                let data2 = item2[sort.active]
                if ( data1!==undefined && data2!==undefined) {
                    if (typeof(data1) === 'string' && typeof(data2) === 'string') {
                        if ( sort.direction == 'asc') {
                            result = data1.localeCompare(data2)
                        } else {
                            result = data2.localeCompare(data1)
                        }
                    } else if (typeof(data1) === 'number' && typeof(data2) === 'number') {
                        if ( sort.direction == 'asc') {
                            result = data1 - data2;
                        } else {
                            result = data2 - data1;
                        }
                    } else if (typeof(data1) === 'boolean' && typeof(data2) === 'boolean') {
                        if ( data1 === data2) {
                            result = 0;
                        } else if ( sort.direction == 'asc') {
                            result = data1 && !data2 ? 1:-1;
                        } else {
                            result = data1 && !data2 ? -1:1;
                        }
                    }
                }
    
            }
            return result;
        })
        return items;
    }

    skip: number = 0;
    take: number;
    dataLength: number = 0
    searchStr: string = ''
    onlyEditedRows: boolean=false
    sort: Sort | undefined
}
