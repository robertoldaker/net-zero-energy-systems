import { Component, Input, OnInit } from '@angular/core';
import { LoadflowDataComponent } from '../loadflow-data/loadflow-data.component';

@Component({
    selector: 'app-loadflow-node-cell',
    templateUrl: './loadflow-node-cell.component.html',
    styleUrls: ['./loadflow-node-cell.component.css']
})
export class LoadflowNodeCellComponent implements OnInit {

    constructor(
        private dataComponent: LoadflowDataComponent,
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
