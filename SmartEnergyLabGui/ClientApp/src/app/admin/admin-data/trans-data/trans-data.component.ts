import { Component, OnInit, ViewChild } from '@angular/core';
import { MatSort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { DataModel, DistributionDataRow, LoadNetworkDataSource, NationalGridNetworkSource, TransmissionDataRow } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogFooterButtonsEnum } from 'src/app/dialogs/dialog-footer/dialog-footer.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { MessageDialogIcon } from 'src/app/dialogs/message-dialog/message-dialog.component';

@Component({
    selector: 'app-trans-data',
    templateUrl: './trans-data.component.html',
    styleUrls: ['./trans-data.component.css']
})
export class TransDataComponent implements OnInit {

    constructor(private dataService: DataClientService, private dialogService: DialogService) {
        this.displayedColumns = ['sourceStr', 'numLocations','numSubstations', 'buttons']
        this.tableData = new MatTableDataSource()
        this.refresh();
    }

    ngOnInit(): void {
    }

    tableData: MatTableDataSource<TransmissionDataRow>
    @ViewChild(MatSort) 
    sort: MatSort | null = null;

    displayedColumns: string[]

    sortDataAccessor(data: any, headerId: string): number | string {
        return data[headerId];
    }

    loadNetworkData(source: NationalGridNetworkSource) {
        this.dataService.NationalGridLoadNetwork(source, (result)=>{
            this.refresh();
        })
    }

    refresh() {
        this.dataService.GetTransmissionData((dm)=>{
            this.tableData = new MatTableDataSource(dm.rows)
            this.tableData.sortingDataAccessor = this.sortDataAccessor
            this.tableData.sort = this.sort
        });
    }

    deleteAll(row: TransmissionDataRow) {
        this.dialogService.showMessageDialog(
            {
                message: `<div><div>This command will delete [<b>${row.numLocations}</b>] locations, [<b>${row.numSubstations}</b>] substations </div><div>&nbsp;</div><div>Continue?</div></div>`,
                icon: MessageDialogIcon.Warning,
                buttons: DialogFooterButtonsEnum.OKCancel
            },
            ()=>{
                this.dataService.NationalGridDeleteNetwork(row.source,()=>{
                    this.refresh()
                });
            }
        )
    }

    Source = NationalGridNetworkSource




}
