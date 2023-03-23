import { AfterViewInit, Component, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { ElsiLink, TableInfo } from 'src/app/data/app.data';
import { CellEditorData, ICellEditorDataDict } from 'src/app/utils/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { ElsiDataService } from '../elsi-data.service';

@Component({
    selector: 'app-elsi-links',
    templateUrl: './elsi-links.component.html',
    styleUrls: ['./elsi-links.component.css']
})
export class ElsiLinksComponent extends ComponentBase implements OnInit, AfterViewInit {

    constructor(public service: ElsiDataService) {
        super()
        this.sort = null
        this.displayedColumns = []
        if ( this.service.datasetInfo) {
            this.tableData = this.createDataSource(this.service.datasetInfo.linkInfo)
        } else {
            this.tableData = this.createDataSource({data:[],userEdits: [], tableName: '' })
        }
        this.addSub( this.service.DatasetInfoChange.subscribe( (ds) => {
            this.tableData = this.createDataSource(ds.linkInfo)
        }))
    }
    ngAfterViewInit(): void {
        if ( this.tableData ) {
            this.tableData.sort = this.sort;
        }
    }

    ngOnInit(): void {
    }

    private createDataSource(tableInfo: TableInfo<ElsiLink>):MatTableDataSource<ICellEditorDataDict> {
        if ( this.displayedColumns.length === 0) {
            //?? Can't guarantee order to need to spell out explicitly
            //??let colNames = tableInfo.data.length>0 ? Object.getOwnPropertyNames(tableInfo.data[0]) : []
            //??this.displayedColumns = colNames.filter(m=>m!=="id" && m!=="fromZone" && m!=="toZone");
            this.displayedColumns = ['name','fromZoneStr','toZoneStr','capacity','revCap','loss','market','itf','itt','btf','btt']
        }
        let versionId: number = this.service.dataset ? this.service.dataset.id : 0
        let cellData = CellEditorData.GetCellDataObjects<ElsiLink>(tableInfo,(item)=>item.name, versionId)
        let td = new MatTableDataSource(cellData)
        td.sortingDataAccessor =this.sortDataAccessor
        if ( this.sort ) {
            td.sort = this.sort
        }
        return td
    }
    

    displayedColumns: string[]
    tableData: MatTableDataSource<any>
    @ViewChild(MatSort) sort: MatSort | null;


    getId(index: number, item: ElsiLink) {
        return item.id
    }

    sortDataAccessor(data:ICellEditorDataDict, headerId: string) : number | string {
        return data[headerId].value;
    }

    get isReadOnly() {
        return this.service.isReadOnly
    }

}
