import { Component, Input, OnInit } from '@angular/core';
import { Node } from '../../../data/app.data'
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { DialogService } from 'src/app/dialogs/dialog.service';

@Component({
    selector: 'app-loadflow-node-info-table',
    templateUrl: './loadflow-node-info-table.component.html',
    styleUrls: ['./loadflow-node-info-table.component.css']
})
export class LoadflowNodeInfoTableComponent implements OnInit {

    constructor(private dataService: LoadflowDataService, private dialogService: DialogService) { }

    ngOnInit(): void {
    }

    @Input()
    nodes:Node[] = []

    getGeneration(node: Node):number {
        //return this.dataService.transportModel===TransportModelOld.PeakSecurity ? node.generation_A : node.generation_B
        return 0
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
        this.dialogService.showLoadflowNodeDialog(itemData)
    }
}
