import { Component, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { DataRow } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
  selector: 'app-admin-data',
  templateUrl: './admin-data.component.html',
  styleUrls: ['./admin-data.component.css']
})
export class AdminDataComponent extends ComponentBase {
    
    constructor(private dataService: DataClientService) {
        super()
        this.sort = null
        this.displayedColumns = ['geoGraphicalArea','dno','numGsps','numPrimary','numDist']
        this.rows = []
        this.dataService.DataModel((dm)=>{
            this.rows = dm.rows
            this.tableData = new MatTableDataSource(this.rows)
            this.tableData.sortingDataAccessor = this.sortDataAccessor
            this.tableData.sort = this.sort

        });
        this.tableData = new MatTableDataSource(this.rows)
    }

    tableData: MatTableDataSource<DataRow>
    @ViewChild(MatSort) sort: MatSort | null;

    rows: DataRow[];

    displayedColumns: string[]

    sortDataAccessor(data: any, headerId: string): number | string {
        return data[headerId];
    }

}
