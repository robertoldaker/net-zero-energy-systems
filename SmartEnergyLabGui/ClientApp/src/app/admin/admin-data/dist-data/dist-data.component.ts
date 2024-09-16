import { Component, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { DataModel, DistributionDataRow, DistributionData, LoadNetworkDataSource } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogFooterButtonsEnum } from 'src/app/dialogs/dialog-footer/dialog-footer.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { MessageDialogIcon } from 'src/app/dialogs/message-dialog/message-dialog.component';

@Component({
    selector: 'app-dist-data',
    templateUrl: './dist-data.component.html',
    styleUrls: ['./dist-data.component.css']
})
export class DistDataComponent implements OnInit {

    constructor(private dataService: DataClientService, private dialogService: DialogService) { 
        this.sort = null
        this.displayedColumns = ['geoGraphicalArea','dno','numGsps','numPrimary','numDist','buttons']
        this.tableData = new MatTableDataSource()
        this.refresh();
    }

    ngOnInit(): void {
    }

    tableData: MatTableDataSource<DistributionDataRow>
    @ViewChild(MatSort) sort: MatSort | null;

    model: DistributionData | undefined;

    displayedColumns: string[]

    sortDataAccessor(data: any, headerId: string): number | string {
        return data[headerId];
    }

    loadData(source: LoadNetworkDataSource) {
        this.dataService.LoadDistributionData(source, (result)=>{
        })
    }

    refresh() {
        this.dataService.GetDistributionData((dm)=>{
            this.tableData = new MatTableDataSource(dm.rows)
            this.tableData.sortingDataAccessor = this.sortDataAccessor
            this.tableData.sort = this.sort
        });
    }

    deleteAll(row: DistributionDataRow) {
        this.dialogService.showMessageDialog(
            {
                message: `<div><div>This command will delete [<b>${row.numGsps}</b>] GSPs, [<b>${row.numPrimary}</b>] primary substations and [<b>${row.numDist}</b>] distribution substations.</div><div>&nbsp;</div><div>Continue?</div></div>`,
                icon: MessageDialogIcon.Warning,
                buttons: DialogFooterButtonsEnum.OKCancel
            },
            ()=>{
                this.dataService.DeleteAllSubstations(row.geoGraphicalAreaId,`Deleting all from [${row.geoGraphicalArea}] ...`,()=>{
                    this.refresh()
                });
            }
        )
    }

    LoadNetworkDataSource = LoadNetworkDataSource

}
