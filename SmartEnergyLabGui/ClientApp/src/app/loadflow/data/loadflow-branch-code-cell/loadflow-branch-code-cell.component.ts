import { Component, Input, OnInit } from '@angular/core';
import { LoadflowDataComponent } from '../loadflow-data/loadflow-data.component';

@Component({
    selector: 'app-loadflow-branch-code-cell',
    templateUrl: './loadflow-branch-code-cell.component.html',
    styleUrls: ['./loadflow-branch-code-cell.component.css']
})
export class LoadflowBranchCodeCellComponent implements OnInit {

    constructor(private dataComponent: LoadflowDataComponent) { }

    ngOnInit(): void {
    }

    @Input()
    branchCode: string = ""

    @Input()
    node1LocationId: number = 0

    @Input()
    node2LocationId: number = 0

    @Input()
    ctrlId: number = 0

    @Input()
    isCtrl: boolean = false

    showLinkOnMap() {
        if ( this.node1LocationId == this.node2LocationId ) { 
            this.dataComponent.showLocationOnMapById(this.node1LocationId)
        } else {
            this.dataComponent.showBranchOnMap(this.node1LocationId,this.node2LocationId)
        }
    }

    showCtrlOrBranch() {
        if ( !this.isCtrl && this.ctrlId > 0) {
            this.dataComponent.showCtrl(this.branchCode)
        } else if ( this.isCtrl ) {
            this.dataComponent.showBranch(this.branchCode)
        }
    }

    get codeClass() {
        return (!this.isCtrl && this.ctrlId > 0) || this.isCtrl ? 'cellLink' : ''
    }

}
