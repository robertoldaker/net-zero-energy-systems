import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ElsiDataVersion } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogBase } from 'src/app/dialogs/diaglog-base';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';
import { ElsiDataService } from '../elsi-data.service';

@Component({
    selector: 'app-elsi-dataset-dialog',
    templateUrl: './elsi-dataset-dialog.component.html',
    styleUrls: ['./elsi-dataset-dialog.component.css']
})
export class ElsiDatasetDialogComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<ElsiDatasetDialogComponent>, 
        @Inject(MAT_DIALOG_DATA) private data:(ElsiDataVersion|null),
        private service: DataClientService, 
        private messageService: ShowMessageService, 
        public dataService: ElsiDataService) {
        super()
        let fc = this.addFormControl('name')
        if ( data ) {
            fc.setValue(data.name)
        }
    }

    ngOnInit(): void {
    }

    get title():string {
        return this.data ? "Edit dataset" : "New dataset"
    }

    get isNew():boolean {
        return !this.data;
    }

    save() {
        if ( this.data ) {
            // edit            
            let data = Object.assign({},this.form.value);
            data.id = this.data.id
            data.parent = null
            this.service.SaveElsiDataVersion(data,()=>{
                this.dialogRef.close();
                this.messageService.showMessageWithTimeout("Dataset successfully saved")
                if ( this.data ) {
                    this.dataService.loadDataVersions(this.data.id)
                }
            }, (errors)=>{
                this.fillErrors(errors)
            })    
        } else {
            // new 
            let data = this.form.value
            data.parentId = this.dataService.dataset?.id;
            this.service.NewElsiDataVersion(data,(id)=>{
                this.dialogRef.close();
                this.messageService.showMessageWithTimeout("New dataset successfully saved")
                this.dataService.loadDataVersions(parseInt(id))
            }, (errors)=>{
                this.fillErrors(errors)
            })    
        }
    }

}
