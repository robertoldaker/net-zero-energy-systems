import { Component, Inject, Input, OnInit } from '@angular/core';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
    selector: 'app-gsp-header',
    templateUrl: './gsp-header.component.html',
    styleUrls: ['./gsp-header.component.css']
})
export class GspHeaderComponent implements OnInit {

    constructor(private dialogService: DialogService, @Inject('MODE') public mode: string) { }

    @Input() title: string = ""

    ngOnInit(): void {
    }

}
