import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Dataset, Node } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { DatasetDialogComponent } from 'src/app/datasets/dataset-dialog/dataset-dialog.component';
import { DialogBase } from 'src/app/dialogs/diaglog-base';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';

@Component({
    selector: 'app-loadflow-node-dialog',
    templateUrl: './loadflow-node-dialog.component.html',
    styleUrls: ['./loadflow-node-dialog.component.css']
})
export class LoadflowNodeDialogComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>, 
        @Inject(MAT_DIALOG_DATA) public data:Node,
        private service: DataClientService, 
        private messageService: ShowMessageService
    ) { 
        super()
        this.title = `Edit node [${data.code}]`
        let fc = this.addFormControl('code')
        if ( this.data.code ) {
            fc.setValue(this.data.code)
        }
    }

    ngOnInit(): void {
    }    

    title: string
    voltages:number[] = [33,66,132,275,400]

    save() {

    }

}
