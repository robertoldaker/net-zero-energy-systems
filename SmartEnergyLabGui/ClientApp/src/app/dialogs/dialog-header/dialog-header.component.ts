import { Component, Input, OnInit } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-dialog-header',
  templateUrl: './dialog-header.component.html',
  styleUrls: ['./dialog-header.component.css']
})
export class DialogHeaderComponent implements OnInit {

  constructor() { 

  }

  ngOnInit(): void {
  }

  @Input()
  dialogRef: any | undefined 

  close() {
      if ( this.dialogRef!==undefined) {
          this.dialogRef.close();
      }
  }

}
