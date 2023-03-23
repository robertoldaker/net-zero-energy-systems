import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { ElsiPeakDemand, ElsiScenario, TableInfo } from 'src/app/data/app.data';
import { CellEditorData, ICellEditorDataDict } from 'src/app/utils/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { ElsiDataService } from '../elsi-data.service';

@Component({
    selector: 'app-elsi-demands',
    templateUrl: './elsi-demands.component.html',
    styleUrls: ['./elsi-demands.component.css']
})
export class ElsiDemandsComponent extends ComponentBase implements OnInit, AfterViewInit {

    constructor(public service: ElsiDataService) {
        super()
        this.sort = null
        this.displayedColumns = ['zoneStr', 'profileStr', 'communityRenewables', 'twoDegrees', 'steadyProgression', 'consumerEvolution']
        if (this.service.datasetInfo) {
            this.tableData = this.createDataSource(this.service.datasetInfo.peakDemandInfo)
        } else {
            this.tableData = this.createDataSource({ data: [], userEdits: [], tableName: '' })
        }
        this.addSub(this.service.DatasetInfoChange.subscribe((ds) => {
            this.tableData = this.createDataSource(ds.peakDemandInfo)
        }))
    }
    ngAfterViewInit(): void {
        if (this.tableData) {
            this.tableData.sort = this.sort;
        }
    }

    ngOnInit(): void {

    }

    private createDataSource(tableInfo: TableInfo<ElsiPeakDemand>):MatTableDataSource<ICellEditorDataDict> {
        let versionId: number = this.service.dataset ? this.service.dataset.id : 0
        let cellData = this.getCellDataObjects(tableInfo, versionId)
        let td = new MatTableDataSource(cellData)
        td.sortingDataAccessor =this.sortDataAccessor
        if ( this.sort ) {
            td.sort = this.sort
        }
        return td
    }

    private getCellDataObjects(tableInfo: TableInfo<ElsiPeakDemand>, versionId: number):ICellEditorDataDict[] {
        let cellData:ICellEditorDataDict[] = []
        let data = tableInfo.data.filter(m=>m.mainZoneStr);
        let items = this.getTableArray(data);

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

    private getTableArray(items: ElsiPeakDemand[]): ElsiPeakDemandTable[] {
        let tableItems:Map<string,ElsiPeakDemandTable> = new Map();
        items.forEach(item=>{
            let tableItem:ElsiPeakDemandTable
            let ti = tableItems.get(item.mainZoneStr);
            if ( ti ) {
                tableItem = ti
            } else {
                tableItem = new ElsiPeakDemandTable(item)
                tableItems.set(item.mainZoneStr,tableItem)
            }
            tableItem.update(item)
        })
        let tableArray = new Array<ElsiPeakDemandTable>(tableItems.size);
        let index = 0;
        tableItems.forEach( (m)=>tableArray[index++] = m)
        return tableArray;
    }



    displayedColumns: string[]
    tableData: MatTableDataSource<any>
    @ViewChild(MatSort) sort: MatSort | null;


    getId(index: number, item: ElsiPeakDemandTable) {
        return item.zoneStr
    }

    sortDataAccessor(data: ICellEditorDataDict, headerId: string): number | string {
        return data[headerId].value;
    }

    get isReadOnly() {
        // 
        return this.service.isReadOnly
    }


}


export class ElsiPeakDemandTable {
    constructor(item: ElsiPeakDemand) {
        this.zoneStr = item.mainZoneStr
        this.profileStr = item.profileStr
    }
    zoneStr: string
    profileStr: string
    communityRenewables: number | undefined
    twoDegrees: number | undefined
    steadyProgression: number | undefined
    consumerEvolution: number| undefined
    update(item: ElsiPeakDemand) {
        if ( item.scenario == ElsiScenario.CommunityRenewables) {
            this.communityRenewables = item.peak
        } else if ( item.scenario == ElsiScenario.ConsumerEvolution) {
            this.consumerEvolution = item.peak
        } else if ( item.scenario == ElsiScenario.SteadyProgression) {
            this.steadyProgression = item.peak
        } else if ( item.scenario == ElsiScenario.TwoDegrees) {
            this.twoDegrees = item.peak
        }
    }
    getKey(col: string):string {
        if (col == 'communityRenewables') {
            return `${this.zoneStr}:${this.profileStr}:CommunityRenewables`
        } else if ( col == 'consumerEvolution') {
            return `${this.zoneStr}:${this.profileStr}:ConsumerEvolution`
        } else if ( col == 'steadyProgression') {
            return `${this.zoneStr}:${this.profileStr}:SteadyProgression`
        } else if ( col == 'twoDegrees') {
            return `${this.zoneStr}:${this.profileStr}:TwoDegrees`
        } else {
            return this.zoneStr
        }
    }
    getCol(col: string):string {
        let defaultName = 'peak';
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

