import { Component, ElementRef, EventEmitter, Input, OnInit, Output, ViewChild } from '@angular/core';
import { Sort, SortDirection } from '@angular/material/sort';
import { Dataset, DatasetData, DatasetType, IId, UserEdit } from 'src/app/data/app.data';
import { DatasetsService } from 'src/app/datasets/datasets.service';
import { DataTableBaseComponent } from '../data-table-base/data-table-base.component';

@Component({
    selector: 'app-cell-editor',
    templateUrl: './cell-editor.component.html',
    styleUrls: ['./cell-editor.component.css']
})
export class CellEditorComponent {

    constructor(private datasetsService: DatasetsService) {

    }

    ngOnInit(): void {
        if ( this.enumeration) {
            this.options = []
            for (const key in this.enumeration) {
                if (isNaN(Number(key))) { // Ensure we only get string keys
                    this.options.push(key)
                }
            }
        }

    }

    onFocus() {
        this.hasFocus = true
    }

    onBlur(e: any) {
        this.hasFocus = false;
        if ( this.changed) {
            this.saveEdit()
        } else {
            this.clear()
        }
    }

    private clear() {
        if ( this.input) {
            this.input.nativeElement.value = this.value
            this.error = ''
            this.changed = false
        }
    }

    onChange(e: any) {
        this.changed = true;
    }

    onChangeCheckBox(e: any) {
        let value = e.target.checked
        this.saveValue(value)
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
        this.clear()
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
                this.error = error[this.data.columnName]
                if ( this.input) {
                    this.input.nativeElement.focus()
                }
            })
        }
    }

    isSelected(v: string) {
        if ( this.enumeration) {
            return this.enumeration[v] === this.data.value
        } else {
            return v === this.data.value
        }
    }

    saveSelect(e: any) {
        if ( this.enumeration) {
            this.saveValue(this.enumeration[e.target.value]);
        } else {
            this.saveValue(e.target.value);
        }
    }

    delete() {
        if ( this.data && this.canDelete) {
            this.datasetsService.removeUserEditWithPrompt(this.data, (resp)=>{
                this.onEdited.emit(this.data)
            })
        }
    }

    hasFocus: boolean = false
    changed: boolean = false

    @Input()
    data: CellEditorData = new CellEditorData({id: 0, name: '',parent: null, type: DatasetType.Elsi, isReadOnly: true})

    @Input()
    scalingFac: number | undefined

    @Input()
    decimalPlaces: number | undefined

    @Input()
    canDelete: boolean = true

    get readOnly(): boolean {
        return this.data.dataset.isReadOnly || this.data.isRowDeleted
    }

    @Input()
    options: string[] | undefined

    @Input()
    enumeration: any

    @Input()
    checkBox: boolean = false

    @Output()
    onEdited: EventEmitter<CellEditorData> = new EventEmitter<CellEditorData>()

    get value():any {
        let value = this.data?.value
        if ( this.enumeration) {
            value = this.enumeration[value]
        } else if ( typeof value == "number" ) {
            if ( this.scalingFac ) {
                value=value*this.scalingFac
            }
            if ( this.decimalPlaces!==undefined ) {
                value = value.toFixed(this.decimalPlaces)
            }
        }
        return value
    }

    get prevValue():string {
        let pv = this.data.userEdit?.prevValue
        if ( pv) {
            let pvFloat = parseFloat(pv)
            if ( !isNaN(pvFloat)) {
                return pvFloat.toFixed(this.decimalPlaces)
            }  else {
                return pv;
            }
        } else {
            return ""
        }
    }

    get inputClass():string {
        let classes = this.data.userEdit ? 'existing' : ''
        if ( typeof this.data?.value == 'number' && !this.enumeration) {
            classes+=" numeric"
        } else {
            classes+=" text"
        }
        return classes
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

        // filter by search string
        if ( this.searchStr ) {
            items = this.filterDataByStr(items,columnNames)
        }

        // filter by column value
        items = this.filterDataByColumn(items,columnNames)

        // filter by custom filter fcn
        if ( this.customFilter) {
            items = this.filterData(items,this.customFilter)
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

    private filterDataByStr(items: any[], columnNames: string[]):any[] {
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

    private filterData(items: any[], customFilter:ICustomDataFilter ):any[] {
        items = items.filter(item=>{
            return customFilter.filterFcn(customFilter, item)
        })
        return items;
    }

    private filterDataByColumn(items: any[], columnNames: string[] ):any[] {
        // look for columns that have an enabled filter
        let colNames = columnNames.filter( m=>this.columnFilterMap.get(m)?.enabled )
        if ( colNames.length>0 ) {
            items = items.filter(item=>{
                let filter = true;
                colNames.forEach(col=>{
                    let colFilter = this.columnFilterMap.get(col)
                    let result = colFilter?.filterFcn(item,colFilter)
                    if ( filter && result ) {
                        filter = true;
                    } else {
                        filter = false
                    }
                })
                return filter
            })
        }
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

    public addCustomSorter(column: string, sortFcn: (col: string, item1: any,item2: any)=>number) {
        this.sortFcnMap.set(column,sortFcn)
    }

    private sortFcnMap: Map<string,(col: string, item1: any,item2: any)=>number> = new Map()

    private sortData(items: any[]):any[] {
        if ( this.sort) {
            let sortFcnTest = this.sortFcnMap.get(this.sort.active)
            let sortFcn = sortFcnTest ? sortFcnTest : this.defaultSortFcn
            let col:string = this.sort.active
            items.sort((item1,item2) => {
                let result = sortFcn(col,item1,item2)
                return this.sort?.direction === 'asc' ? result : -result
            })
        }
        return items;
    }

    private defaultSortFcn(col: string, item1: any,item2: any):number {
        let result = 0;
        let data1 = item1[col]
        if ( data1===undefined || data1===null) {
            return -1
        }
        let data2 = item2[col]
        if ( data2===undefined || data2===null) {
            return 1
        }
        if ( data1!==undefined && data2!==undefined) {
            if (typeof(data1) === 'string' && typeof(data2) === 'string') {
                result = data1.localeCompare(data2)
            } else if (typeof(data1) === 'number' && typeof(data2) === 'number') {
                result = data1 - data2;
            } else if (typeof(data1) === 'boolean' && typeof(data2) === 'boolean') {
                if ( data1 === data2) {
                    result = 0;
                } else {
                    result = data1 && !data2 ? 1:-1;
                }
            } else {
                console.log('unexpected value for data1, data2')
            }
        }
        return result;
    }

    public reset(clearFilters?: boolean) {
        this.skip = 0
        this.searchStr = ''
        this.onlyEditedRows = false
        if ( clearFilters ) {
            this.clearFilters()
        }
    }

    public clearFilters() {
        this.columnFilterMap.forEach((v,k)=>{
            v.enabled = false
        })
    }

    skip: number = 0;
    take: number;
    dataLength: number = 0
    searchStr: string = ''
    onlyEditedRows: boolean=false
    sort: Sort | undefined
    customFilter: ICustomDataFilter | undefined
    columnFilterMap: Map<string,ColumnDataFilter> = new Map()
}

export interface ICustomDataFilter {
    filterFcn: (customFilter: any, value: any)=>boolean
}

export class ColumnDataFilter {
    constructor(private baseComponent: DataTableBaseComponent<any>, public columnName:string, values?:any[], enumerator?: any ) {
        if ( values) {
            this.values = values
            this.auto = false
        } else {
            this.values = []
            this.auto = true
        }
        if ( enumerator) {
            this.enumerator = enumerator
        }
    }
    value: any
    enabled: boolean = false
    auto: boolean = true
    values: any[] = []
    enumerator: any // enumeration type to map value to a string representation
    filterFcn: (item: any, colFilter: ColumnDataFilter)=>boolean = (item,colFilter) => (colFilter.value == item[colFilter.columnName])
    enable(value: any) {
        if ( this.enumerator ) {
            this.value = this.enumerator[value]
        } else {
            this.value = value
        }
        this.enabled = true
        this.baseComponent.newFilterTable()
    }
    get selectedValue():any {
        if ( this.enumerator ) {
            return this.enumerator[this.value]
        } else {
            return this.value
        }
    }

    disable() {
        this.enabled = false
        this.baseComponent.newFilterTable()
    }

    genValues(items: any[]) {
        if ( this.auto ) {
            let colItems:any[]
            if ( this.enumerator) {
                colItems = items.map(m=>this.enumerator[m[this.columnName]])
            } else {
                colItems = items.map(m=>m[this.columnName])
            }

            let set = new Set(colItems)
            this.values = [...set]
            if ( this.values.length>0) {
                if ( typeof(this.values[0]) === 'number') {
                    this.values.sort((a,b)=>b-a) // sorts descending numerically
                } else {
                    this.values.sort()
                }
            }
        }

    }

}
