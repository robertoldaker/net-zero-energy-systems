import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Dataset, DatasetData, ElsiPeakDemand, ElsiScenario } from 'src/app/data/app.data';
import { CellEditorData, DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { ElsiDataService } from '../elsi-data.service';
import { TablePaginatorComponent } from 'src/app/datasets/table-paginator/table-paginator.component';

@Component({
    selector: 'app-elsi-demands',
    templateUrl: './elsi-demands.component.html',
    styleUrls: ['./elsi-demands.component.css']
})
export class ElsiDemandsComponent extends ComponentBase {

    constructor(public service: ElsiDataService) {
        super()
        this.displayedColumns = ['zoneStr', 'profileStr', 'communityRenewables', 'twoDegrees', 'steadyProgression', 'consumerEvolution']
        if (this.service.datasetInfo) {
            this.createDataSource(this.service.datasetInfo.peakDemandInfo)
        } 
        this.addSub(this.service.DatasetInfoChange.subscribe((ds) => {
            this.createDataSource(ds.peakDemandInfo)
        }))
    }

    private createDataSource(datasetData?: DatasetData<ElsiPeakDemand>) {
        if ( datasetData ) {
            let data = datasetData.data.filter(m=>m.mainZoneStr);
            let items = this.getTableArray(data);
            this.datasetData = { data: items, tableName: datasetData.tableName, userEdits: datasetData.userEdits, deletedData: []}
        }
        if ( this.datasetData) {
            let cellData = this.dataFilter.GetCellDataObjects<ElsiPeakDemandTable>(
                this.service.dataset,
                this.datasetData,
                (item,col)=>item.getKey(col),
                (col)=>ElsiPeakDemandTable.getCol(col)
            )
            this.tableData = new MatTableDataSource(cellData)    
        }
    }

    private getCellDataObjects(datasetData: DatasetData<ElsiPeakDemand>, dataset: Dataset):ICellEditorDataDict[] {
        let cellData:ICellEditorDataDict[] = []
        let data = datasetData.data.filter(m=>m.mainZoneStr);
        let items = this.getTableArray(data);

        let columnNames = items.length>0 ? Object.getOwnPropertyNames(items[0]) : [];

        items.forEach( item=>{
            let data:any = item
            let cellObj:any = {}
            columnNames.forEach(col=>{
                let cd = new CellEditorData(dataset)
                cd.key = item.getKey(col)
                //cd.columnName = item.getCol(col)
                cd.tableName = datasetData.tableName
                cd.value = data[col]
                cellObj[col] = cd
                // Find existing userEdit
                cd.userEdit = datasetData.userEdits.find(m=>m.columnName==cd.columnName && m.key==cd.key)
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

    @ViewChild(TablePaginatorComponent)
    tablePaginator: TablePaginatorComponent | undefined    
    dataFilter: DataFilter = new DataFilter(20)
    datasetData?: DatasetData<ElsiPeakDemandTable>
    displayedColumns: string[]
    tableData: MatTableDataSource<any> = new MatTableDataSource()

    getId(index: number, item: ElsiPeakDemandTable) {
        return item.zoneStr
    }

    filterTable(e: DataFilter) {
        this.createDataSource()
    }

    sortTable(e:Sort) {
        this.dataFilter.sort = e
        this.createDataSource()
        this.tablePaginator?.firstPage()
    }
}


export class ElsiPeakDemandTable {
    constructor(item: ElsiPeakDemand) {
        this.zoneStr = item.mainZoneStr
        this.profileStr = item.profileStr
    }
    //?? Needed to get GetCellDataObjects - but needs looking into!
    id: number = 0
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
    static getCol(col: string):string {
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

