import { Component, Inject, Input, OnInit } from '@angular/core';
import { DialogService } from '../../dialogs/dialog.service';

@Component({
    selector: 'app-boundcalc-header',
    templateUrl: './boundcalc-header.component.html',
    styleUrls: ['./boundcalc-header.component.css']
})
export class BoundCalcHeaderComponent implements OnInit {

    constructor(private dialogService: DialogService, @Inject('MODE') public mode: string) { }

    ngOnInit(): void {
    }
    @Input() title: string = ""

    showAboutDialog() {
        this.dialogService.showAboutBoundCalcDialog();
    }
    
    showHelpDialog() {
        this.dialogService.showHelpBoundCalcDialog();
    }
}
