import { Component, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { User } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { MainService } from 'src/app/main/main.service';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
    selector: 'app-admin-users',
    templateUrl: './admin-users.component.html',
    styleUrls: ['./admin-users.component.css']
})
export class AdminUsersComponent extends ComponentBase implements OnInit {

    constructor(private dataService: DataClientService, private mainService:MainService) {
        super()
        this.sort = null
        this.displayedColumns = ['isConnected','name','email','role','enabled']
        this.users = []
        this.tableData = new MatTableDataSource(this.users)
        this.loadUsers()
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
        let d = data[headerId];
        if ( typeof(d) === "string") {
            return d.toLocaleLowerCase();
        } else {
            return d;
        }
    }

    reload() {
        this.loadUsers();
    }

    private loadUsers() {
        this.dataService.GetUsers((users) => {
            if ( this.onlyConnectedUsers) {
                this.users = users.filter(m=>m.numConnections>0)
            } else {
                this.users = users
            }
            this.tableData = new MatTableDataSource(this.users)
            this.tableData.sortingDataAccessor = this.sortDataAccessor
            this.tableData.sort = this.sort

        });
    }

    onlyConnectedUsersClicked() {
        this.loadUsers()
    }

    onlyConnectedUsers: boolean = false

    pingUsers() {
        this.mainService.pingUsers()
    }

}
