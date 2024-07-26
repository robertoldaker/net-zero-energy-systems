import { AfterViewInit, Component, ElementRef, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { Sort } from '@angular/material/sort';
import { Dataset, DatasetData, DatasetType, IId, UserEdit } from 'src/app/data/app.data';
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

    onBlur(e: any) {
        this.hasFocus = false;
        if ( this.input) {
            this.input.nativeElement.value = this.value            
            this.error = ''
        }
    }

    keyDown(e: any) {
        if ( e.keyCode == 13) {
            this.saveEdit()
        } else if ( e.keyCode == 27) {
            this.cancelEdit()
        }
    }

    private cancelEdit() {
        if ( this.input) {
            this.input.nativeElement.blur()
        }
    }

    mouseDown(e: any) {
        // stops the input losing focus
        e.preventDefault()
    }

    cancel(e: Event) {
        e.stopPropagation()
        if ( this.input) {
            this.input.nativeElement.blur()
        }
    }

    @ViewChild('input')
    input: ElementRef | undefined;

    error: string = ''

    save(e: Event) {
        e.stopPropagation()
        this.saveEdit()
    }

    private saveEdit() {
        if ( this.input ) {
            let value = this.input.nativeElement.value;
            if ( value===this.value ) {
                this.cancelEdit()
                return;
            }
            let valueDouble = parseFloat(value)
            if ( this.scalingFac && !isNaN(valueDouble)) {
                value = (valueDouble / this.scalingFac).toString()
            }
            this.saveValue(value);    
        }
    }

    private saveValue(value: string) {
        if ( this.data ) {
            this.datasetsService.saveUserEditWithPrompt(value, this.data, (resp)=>{
                this.error = ''
                this.onEdited.emit(this.data)
            }, (error) =>{
                console.log(error)
                this.error = error[this.data.columnName]
                if ( this.input) {
                    this.input.nativeElement.focus()
                }        
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

    hasFocus: boolean = false

    @Input()
    data: CellEditorData = new CellEditorData({id: 0, name: '',parent: null, type: DatasetType.Elsi, isReadOnly: true})

    @Input()
    scalingFac: number | undefined

    @Input()
    decimalPlaces: number | undefined

    get readOnly(): boolean {
        return this.data.dataset.isReadOnly || this.data.isRowDeleted
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
    [index: string]: CellEditorData,
    _data: any
    _isDeleted:any
    _isLocalDataset: any
    _isLocalEdit:any
}

export class CellEditorData {
    constructor(dataset: Dataset) {
        this.key = ''
        this.tableName = ''
        this.columnName = ''
        this.value = ''
        this.isRowDeleted = false
        this.dataset = dataset;
    }
    key: string
    tableName: string
    columnName: string
    dataset: Dataset
    value: number|string
    isRowDeleted: boolean
    userEdit: UserEdit|undefined
}


export class DataFilter {
    constructor(take: number, sort?: Sort) 
    {
        this.take = take;
        this.sort = sort;
    }

    GetCellDataObjects<T extends IId>(dataset: Dataset, datasetData: DatasetData<T>, 
            keyFcn: (arg: T, col: string)=>string, 
            colFcn?: (col: string)=>string):ICellEditorDataDict[] {
        let rowData:ICellEditorDataDict[] = []
        let items:T[] = []
        let deletedItems:T[]=datasetData.deletedData
        for(let dd of datasetData.data) {
            items.push(dd)
        }
        // set columns based on the first element
        let columnNames = items.length>0 ? Object.getOwnPropertyNames(items[0]) : [];
        // this means we have no items so get column names from any deleted items
        if ( columnNames.length==0 ) {
            columnNames = deletedItems.length>0 ? Object.getOwnPropertyNames(deletedItems[0]) : [];
        }
        // filter by edited items first before always adding deleted items
        if ( this.onlyEditedRows) {
            items = this.filterByEdited(dataset, items, columnNames, datasetData,keyFcn,colFcn)
        }
        // add in the deleted objects
        for(let dd of deletedItems) {
            items.push(dd)
        }
        if ( this.searchStr ) {
            items = this.filterData(items,columnNames)
        }
        if ( this.sort && this.sort.active) {
            this.sortData(items)
        }
        this.dataLength = items.length;
        let skip:number = this.skip
        let take:number = this.take
        for( let i=skip;i<items.length;i++) {
            let data:any = items[i]
            let isDeleted:any = deletedItems.find(m=>m.id == data.id)!==undefined
            let isLocalDataset:any = data.datasetId === dataset.id
            let isLocalEdit:any = data.datasetId === dataset.id || isDeleted
            let rowObj:ICellEditorDataDict = {_data: data, _isDeleted: isDeleted, _isLocalEdit: isLocalEdit, _isLocalDataset: isLocalDataset}
            columnNames.forEach(col=>{
                let cd = new CellEditorData(dataset)
                cd.key = keyFcn(data,col)
                cd.columnName = colFcn ? colFcn(col) : col
                cd.tableName = datasetData.tableName
                cd.value = data[col]
                cd.dataset = dataset
                cd.isRowDeleted = isDeleted
                rowObj[col] = cd
                // Find existing userEdit
                cd.userEdit = datasetData.userEdits.find(m=>m.columnName.toLowerCase()==cd.columnName.toLowerCase() && m.key==cd.key)
                if ( cd.userEdit ) {
                    rowObj._isLocalEdit = true
                }
            })
            rowData.push(rowObj)
            if ( take && rowData.length>=take) {
                break;
            }
        }
        return rowData
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

    private filterByEdited<T>(
        dataset: Dataset,
        items: any[], 
        columnNames: string[],
        datasetData: DatasetData<T>,
        keyFcn: (arg: T, col: string)=>string,
        colFcn?: (col: string)=>string
        ):any[] {            
        items = items.filter(item=>{
            let filter = false;
            let data:any = item
            let isLocalEdit:any = data.datasetId === dataset.id
            if ( isLocalEdit ) {
                return true
            }
            columnNames.forEach(col=>{
                let key = keyFcn(item, col)
                let colName = colFcn ? colFcn(col) : col
                if ( datasetData.userEdits.find(m=>m.columnName.toLowerCase()==colName.toLowerCase() && m.key==key) !== undefined) {
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
                if ( data1===undefined || data1===null) {
                    data1=''
                }
                let data2 = item2[sort.active]
                if ( data2===undefined || data2===null) {
                    data2=''
                }
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
    resetPage: boolean = false
}
