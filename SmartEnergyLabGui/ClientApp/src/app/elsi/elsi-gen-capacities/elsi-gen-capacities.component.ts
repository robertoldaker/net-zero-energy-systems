import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { ElsiGenCapacity, ElsiProfile, ElsiScenario, TableInfo } from 'src/app/data/app.data';
import { CellEditorData, ICellEditorDataDict } from 'src/app/utils/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { ElsiDataService } from '../elsi-data.service';

@Component({
    selector: 'app-elsi-gen-capacities',
    templateUrl: './elsi-gen-capacities.component.html',
    styleUrls: ['./elsi-gen-capacities.component.css']
})

export class ElsiGenCapacitiesComponent extends ComponentBase implements OnInit, AfterViewInit {

    constructor(public service: ElsiDataService) {
        super()
        this.sort = null
        this.displayedColumns = ['zoneStr','genTypeStr','profileStr','communityRenewables','twoDegrees','steadyProgression','consumerEvolution']
        if (this.service.datasetInfo) {
            this.tableData = this.createDataSource(this.service.datasetInfo.genCapacityInfo)
        } else {
            this.tableData = this.createDataSource({ data: [], userEdits: [], tableName: '' })
        }
        this.addSub(this.service.DatasetInfoChange.subscribe((ds) => {
            this.tableData = this.createDataSource(ds.genCapacityInfo)
        }))

    }
    ngAfterViewInit(): void {
        if ( this.tableData ) {
            this.tableData.sort = this.sort;
        }
    }

    ngOnInit(): void {
    }

    private createDataSource(tableInfo: TableInfo<ElsiGenCapacity>):MatTableDataSource<ICellEditorDataDict> {
        let versionId: number = this.service.dataset ? this.service.dataset.id : 0
        let cellData = this.getCellDataObjects(tableInfo, versionId)
        let td = new MatTableDataSource(cellData)
        td.sortingDataAccessor =this.sortDataAccessor
        if ( this.sort ) {
            td.sort = this.sort
        }
        return td
    }

    private getCellDataObjects(tableInfo: TableInfo<ElsiGenCapacity>, versionId: number):ICellEditorDataDict[] {
        let cellData:ICellEditorDataDict[] = []
        let items = this.getTableArray(tableInfo.data);

        let columnNames = items.length>0 ? Object.getOwnPropertyNames(items[0]) : [];

        items.forEach( item=>{
            let data:any = item
            let cellObj:any = {}
            columnNames.forEach(col=>{
                let cd = new CellEditorData()
                cd.key = item.getKey(col)
                cd.columnName = item.getCol(col)
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

    private getTableArray(items: ElsiGenCapacity[]): ElsiGenCapacityTable[] {
        let tableItems:Map<string,ElsiGenCapacityTable> = new Map();
        items.forEach(item=>{
            let tableItem:ElsiGenCapacityTable
            let ti = tableItems.get(item.name);
            if ( ti ) {
                tableItem = ti
            } else {
                tableItem = new ElsiGenCapacityTable(item)
                tableItems.set(item.name,tableItem)
            }
            tableItem.update(item)
        })
        let tableArray = new Array<ElsiGenCapacityTable>(tableItems.size);
        let index = 0;
        tableItems.forEach( (m)=>tableArray[index++] = m)
        return tableArray;
    }


    displayedColumns: string[]
    tableData: MatTableDataSource<any>
    @ViewChild(MatSort) sort: MatSort | null;


    getId(index: number, item: ElsiGenCapacityTable) {
        return item.name
    }

    sortDataAccessor(data:ICellEditorDataDict, headerId: string) : number | string {
        return data[headerId].value;
    }

    get isReadOnly() {
        // this will be the default dataset
        return this.service.isReadOnly
    }
}

export class ElsiGenCapacityTable {
    constructor(item: ElsiGenCapacity) {
        this.name = item.name
        this.zoneStr = item.zoneStr
        this.genTypeStr = item.genTypeStr
        this.profileStr = item.profileStr
    }
    name: string
    zoneStr: string
    genTypeStr: string
    profileStr: string
    communityRenewables: number | undefined
    twoDegrees: number | undefined
    steadyProgression: number | undefined
    consumerEvolution: number| undefined
    update(item: ElsiGenCapacity) {
        if ( item.scenario == ElsiScenario.CommunityRenewables) {
            this.communityRenewables = item.capacity
        } else if ( item.scenario == ElsiScenario.ConsumerEvolution) {
            this.consumerEvolution = item.capacity
        } else if ( item.scenario == ElsiScenario.SteadyProgression) {
            this.steadyProgression = item.capacity
        } else if ( item.scenario == ElsiScenario.TwoDegrees) {
            this.twoDegrees = item.capacity
        }
    }
    getKey(col: string):string {
        if (col == 'communityRenewables') {
            return `${this.name}:CommunityRenewables`
        } else if ( col == 'consumerEvolution') {
            return `${this.name}:ConsumerEvolution`
        } else if ( col == 'steadyProgression') {
            return `${this.name}:SteadyProgression`
        } else if ( col == 'twoDegrees') {
            return `${this.name}:TwoDegrees`
        } else {
            return this.name
        }
    }
    getCol(col: string):string {
        let defaultName = 'capacity';
        if (col == 'communityRenewables') {
            return defaultName;
        } else if ( col == 'consumerEvolution') {
            return defaultName;
        } else if ( col == 'steadyProgression') {
            return defaultName;
        } else if ( col == 'twoDegrees') {
            return defaultName;
        } else {
            return col;
        }
    }
}
