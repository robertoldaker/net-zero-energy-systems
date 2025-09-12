import { Component, Input, OnInit } from '@angular/core';
import { Node } from '../../../data/app.data'
import { BoundCalcDataService } from '../../boundcalc-data-service.service';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
    selector: 'app-boundcalc-node-info-table',
    templateUrl: './boundcalc-node-info-table.component.html',
    styleUrls: ['./boundcalc-node-info-table.component.css']
})
export class BoundCalcNodeInfoTableComponent implements OnInit {

    constructor(private dataService: BoundCalcDataService, private dialogService: DialogService) { }

    ngOnInit(): void {
    }

    @Input()
    nodes:Node[] = []

    getGeneration(node: Node):number {
        return node.generation
    }

    get totalDemandStr():string {
        let total = 0
        for(let n of this.nodes ) {
            total+=n.demand
        }
        return total.toFixed(0)
    }

    get totalGenerationStr():string {
        let total = 0
        for(let n of this.nodes ) {
            total+=this.getGeneration(n)
        }
        return total.toFixed(0)
    }

    editNode(node: Node) {
        let itemData = this.dataService.getNodeEditorData(node.id)
        this.dialogService.showBoundCalcNodeDialog(itemData)
    }
}
