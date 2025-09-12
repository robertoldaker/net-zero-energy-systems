import { Component, Input, OnInit } from '@angular/core';
import { BoundCalcDataComponent } from '../boundcalc-data/boundcalc-data.component';

@Component({
    selector: 'app-boundcalc-node-cell',
    templateUrl: './boundcalc-node-cell.component.html',
    styleUrls: ['./boundcalc-node-cell.component.css']
})
export class BoundCalcNodeCellComponent implements OnInit {

    constructor(
        private dataComponent: BoundCalcDataComponent,
    ) { }

    ngOnInit(): void {
    }

    @Input()
    nodeCode: string = ""
    
    @Input()
    nodeName: string = ""

    @Input()
    isBranch: boolean = false

    showForNode() {
        if ( this.isBranch) {
            this.dataComponent.showNode(this.nodeCode)
        } else {
            this.dataComponent.showBranchesForNode(this.nodeCode)
        }
    }

    showLocationOnMap() {
        this.dataComponent.showLocationOnMap(this.nodeName)
    }

    showLocation() {
        this.dataComponent.showLocation(this.nodeName)
    }
}
