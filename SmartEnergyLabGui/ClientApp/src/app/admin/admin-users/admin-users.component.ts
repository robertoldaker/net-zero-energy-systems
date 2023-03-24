import { Component, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { User } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-admin-users',
    templateUrl: './admin-users.component.html',
    styleUrls: ['./admin-users.component.css']
})
export class AdminUsersComponent extends ComponentBase implements OnInit {

    constructor(private dataService: DataClientService) {
        super()
        this.sort = null
        this.displayedColumns = ['name','email','role','enabled']
        this.users = []
        this.dataService.GetUsers((users)=>{
            this.users = users
            this.tableData = new MatTableDataSource(this.users)
            this.tableData.sortingDataAccessor = this.sortDataAccessor
            this.tableData.sort = this.sort

        });
        this.tableData = new MatTableDataSource(this.users)
    }


    tableData: MatTableDataSource<User>
    @ViewChild(MatSort) sort: MatSort | null;


    users: User[]

    getId(index: number, item: User):number {
        return item.id
    }

    ngOnInit(): void {
    }

    ngAfterViewInit(): void {
        if (this.tableData) {
            this.tableData.sort = this.sort;
        }
    }

    displayedColumns: string[]

    sortDataAccessor(data: any, headerId: string): number | string {
        return data[headerId];
    }

}
