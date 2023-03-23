import { Component, Input, OnInit } from '@angular/core';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
    selector: 'app-elsi-header',
    templateUrl: './elsi-header.component.html',
    styleUrls: ['./elsi-header.component.css']
})
export class ElsiHeaderComponent implements OnInit {

    constructor(private dialogService: DialogService) { }

    ngOnInit(): void {
    }

    @Input() title: string = ""

    showAboutDialog() {
        this.dialogService.showAboutElsiDialog();
    }

    showHelpDialog() {
        this.dialogService.showElsiHelpDialog();
    }



}
