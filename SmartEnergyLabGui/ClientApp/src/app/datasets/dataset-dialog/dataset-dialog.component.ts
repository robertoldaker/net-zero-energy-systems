import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Dataset } from 'src/app/data/app.data';
import { DataClientService } from 'src/app/data/data-client.service';
import { DialogBase } from 'src/app/dialogs/dialog-base';
import { ShowMessageService } from 'src/app/main/show-message/show-message.service';

@Component({
    selector: 'app-dataset-dialog',
    templateUrl: './dataset-dialog.component.html',
    styleUrls: ['./dataset-dialog.component.css']
})
export class DatasetDialogComponent extends DialogBase implements OnInit {

    constructor(public dialogRef: MatDialogRef<DatasetDialogComponent>, 
        @Inject(MAT_DIALOG_DATA) public data:{ dataset: Dataset|null, parent: Dataset},
        private service: DataClientService, 
        private messageService: ShowMessageService) {
        super()
        let fc = this.addFormControl('name')
        if ( this.data.dataset ) {
            fc.setValue(this.data.dataset.name)
        }
    }

    ngOnInit(): void {
    }

    get title():string {
        return this.data.dataset ? "Edit dataset" : "New dataset"
    }

    get isNew():boolean {
        return !this.data.dataset;
    }

    save() {
        if ( this.data.dataset ) {
            // edit            
            let data = Object.assign(this.data.dataset,this.form.value);
            //??data.id = this.data.dataset.id
            //??data.parent = null
            this.service.SaveDataset(data,()=>{
                this.messageService.showMessageWithTimeout("Dataset successfully saved")
                this.dialogRef.close(data.id);
            }, (errors)=>{
                this.fillErrors(errors)
            })    
        } else {
            // new 
            let data = Object.assign({},this.form.value);
            data.parentId = this.data.parent.id;
            this.service.NewDataset(data,(id)=>{
                this.messageService.showMessageWithTimeout("New dataset successfully saved")
                this.dialogRef.close(id);
            }, (errors)=>{
                this.fillErrors(errors)
            })
        }
    }

}

