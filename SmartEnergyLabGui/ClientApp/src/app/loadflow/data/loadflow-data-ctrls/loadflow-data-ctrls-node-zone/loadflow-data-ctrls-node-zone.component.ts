import { Component, Input, OnInit } from '@angular/core';
import { LoadflowCtrlType } from 'src/app/data/app.data';

export enum NodeZoneNum {One,Two}

@Component({
    selector: 'app-loadflow-data-ctrls-node-zone',
    templateUrl: './loadflow-data-ctrls-node-zone.component.html',
    styleUrls: ['./loadflow-data-ctrls-node-zone.component.css']
})
export class LoadflowDataCtrlsNodeZoneComponent implements OnInit {

    constructor() { }

    ngOnInit(): void {
    }

    @Input()
    nodeZoneNum: NodeZoneNum = NodeZoneNum.One

    @Input()
    element: any

    get nodeCode(): string {
        return this.nodeZoneNum == NodeZoneNum.One ? this.element.node1Code.value : this.element.node2Code.value
    }

    get nodeName(): string {
        return this.nodeZoneNum == NodeZoneNum.One ? this.element.node1Name.value : this.element.node2Name.value
    }

    get isNodeCell() : boolean {
        let type = this.element.type.value
        if ( type === LoadflowCtrlType.InterTrip) {
            return this.nodeZoneNum === NodeZoneNum.One ? true : false
        } else if ( type === LoadflowCtrlType.Transfer ) {
            return false
        } else {
            return true
        }
    }

    get zoneCode(): string {
        if ( this.nodeZoneNum == NodeZoneNum.One) {
            return this.element.zone1Code.value
        } else {
            return this.element.zone2Code.value
        }
    }

    get zoneGpc(): number {
        if ( this.nodeZoneNum === NodeZoneNum.One ) {
            return this.element.gpC1.value
        } else {
            return this.element.gpC2.value
        }
    }
}
