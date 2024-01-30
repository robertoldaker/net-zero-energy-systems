import { Component, Inject, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { DataModel, DataRow, LoadNetworkDataSource } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { MessageDialogIcon } from 'src/app/dialogs/message-dialog/message-dialog.component';
import { ComponentBase } from 'src/app/utils/component-base';

@Component({
  selector: 'app-admin-data',
  templateUrl: './admin-data.component.html',
  styleUrls: ['./admin-data.component.css']
})
export class AdminDataComponent extends ComponentBase {
    
    constructor(private dataService: DataClientService, private dialogService: DialogService, @Inject('DATA_URL') private baseUrl: string) {
        super()
        this.inCleanup = false;
        this.sort = null
        this.displayedColumns = ['geoGraphicalArea','dno','numGsps','numPrimary','numDist','buttons']
        this.tableData = new MatTableDataSource()
        this.refresh();
    }

    tableData: MatTableDataSource<DataRow>
    @ViewChild(MatSort) sort: MatSort | null;

    model: DataModel | undefined;

    displayedColumns: string[]

    sortDataAccessor(data: any, headerId: string): number | string {
        return data[headerId];
    }

    loadNetworkData(source: LoadNetworkDataSource) {
        this.dataService.LoadNetworkData(source, (result)=>{
        })
    }

    refresh() {
        this.dataService.DataModel((dm)=>{
            this.model = dm;
            console.log(dm);
            this.tableData = new MatTableDataSource(this.model.rows)
            this.tableData.sortingDataAccessor = this.sortDataAccessor
            this.tableData.sort = this.sort
        });
    }

    backupDb() {
        this.dataService.BackupDb((result)=>{
            console.log(result)
        })
    }

    backupDbLocally() {
        window.location.href = `${this.baseUrl}/Admin/BackupDbLocally`
    }

    performCleanup() {
        this.inCleanup = true;
        this.dataService.PerformCleanup( (result)=>{
            console.log(result)
            this.inCleanup = false;
            this.refresh()
        });
    }

    deleteAll(row: DataRow) {
        this.dialogService.showMessageDialog(
            {
                message: `<div><div>This command will delete [<b>${row.numGsps}</b>] GSPs, [<b>${row.numPrimary}</b>] primary substations and [<b>${row.numDist}</b>] distribution substations.</div><div>&nbsp;</div><div>Continue?</div></div>`,
                icon: MessageDialogIcon.Warning
            },
            ()=>{
                this.dataService.DeleteAllSubstations(row.geoGraphicalAreaId,`Deleting all from [${row.geoGraphicalArea}] ...`,()=>{
                    this.refresh()
                });
            }
        )
    }

    inCleanup: boolean
    LoadNetworkDataSource = LoadNetworkDataSource

}
