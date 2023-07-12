import { Component, Inject, Input, OnInit } from '@angular/core';
import { DialogService } from '../../dialogs/dialog.service';

@Component({
    selector: 'app-loadflow-header',
    templateUrl: './loadflow-header.component.html',
    styleUrls: ['./loadflow-header.component.css']
})
export class LoadflowHeaderComponent implements OnInit {

    constructor(private dialogService: DialogService, @Inject('MODE') public mode: string) { }

    ngOnInit(): void {
    }
    @Input() title: string = ""

    showAboutDialog() {
        this.dialogService.showAboutLoadflowDialog();
    }
    
    showHelpDialog() {
        this.dialogService.showHelpLoadflowDialog();
    }
}
