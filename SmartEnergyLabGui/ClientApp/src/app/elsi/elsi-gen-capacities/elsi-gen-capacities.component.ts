import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { Dataset, DatasetData, ElsiGenCapacity, ElsiProfile, ElsiScenario} from 'src/app/data/app.data';
import { CellEditorData, DataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { ElsiDataService } from '../elsi-data.service';
import { MATERIAL_SANITY_CHECKS } from '@angular/material/core';

@Component({
    selector: 'app-elsi-gen-capacities',
    templateUrl: './elsi-gen-capacities.component.html',
    styleUrls: ['./elsi-gen-capacities.component.css']
})

export class ElsiGenCapacitiesComponent extends ComponentBase {

    constructor(public service: ElsiDataService) {
        super()
        this.displayedColumns = ['zoneStr','genTypeStr','profileStr','communityRenewables','twoDegrees','steadyProgression','consumerEvolution']
        if (this.service.datasetInfo) {
            this.createDataSource(this.service.datasetInfo.genCapacityInfo)
        } 
        this.addSub(this.service.DatasetInfoChange.subscribe((ds) => {
            this.createDataSource(ds.genCapacityInfo)
        }))

    }

    private createDataSource(datasetData?: DatasetData<ElsiGenCapacity>) {
        if ( datasetData ) {
            let items = this.getTableArray(datasetData.data);
            this.datasetData = { data: items, tableName: datasetData.tableName, userEdits: datasetData.userEdits}
        }
        if ( this.datasetData) {
            let cellData = this.dataFilter.GetCellDataObjects<ElsiGenCapacityTable>(
                this.service.dataset,
                this.datasetData,
                (item,col)=>item.getKey(col),
                (col)=>ElsiGenCapacityTable.getCol(col)
            )
            this.tableData = new MatTableDataSource(cellData)    
        }
        return 
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


    dataFilter: DataFilter = new DataFilter(20)
    datasetData?: DatasetData<ElsiGenCapacityTable>
    displayedColumns: string[]
    tableData: MatTableDataSource<any> = new MatTableDataSource()

    getId(index: number, item: ElsiGenCapacityTable) {
        return item.name
    }


    filterTable(e: DataFilter) {
        this.createDataSource()
    }

    sortTable(e:Sort) {
        this.dataFilter.sort = e
        this.createDataSource()
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
    static getCol(col: string):string {
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
