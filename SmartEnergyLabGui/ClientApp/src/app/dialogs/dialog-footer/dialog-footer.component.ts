import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

export enum DialogFooterButtonsEnum { OKCancel, Close }

@Component({
    selector: 'app-dialog-footer',
    templateUrl: './dialog-footer.component.html',
    styleUrls: ['./dialog-footer.component.css']
})
export class DialogFooterComponent implements OnInit {

    constructor() { 
        this.buttons = DialogFooterButtonsEnum.OKCancel
    }

    ngOnInit(): void {
    }
    
    @Input()
    dialogRef: any | undefined

    @Input()
    data: any

    @Input()
    buttons: DialogFooterButtonsEnum

    close() {
        if (this.dialogRef !== undefined) {
            this.dialogRef.close();
        }
    }

    DialogFooterButtons:any = DialogFooterButtonsEnum

    @Output()
    onOk: EventEmitter<any> = new EventEmitter<any>()

    ok() {
        this.onOk?.emit();
    }

}
