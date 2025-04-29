import { Component, Input, OnInit } from '@angular/core';
import { LoadflowDataComponent } from '../../data/loadflow-data/loadflow-data.component';

@Component({
    selector: 'app-loadflow-trip-link',
    templateUrl: './loadflow-trip-link.component.html',
    styleUrls: ['./loadflow-trip-link.component.css']
})
export class LoadflowTripLinkComponent implements OnInit {

    constructor(private dataComponent: LoadflowDataComponent) { }

    ngOnInit(): void {
    }

    @Input()
    set branchName(bn: string) {
        let cpnts = bn.split(':')
        if ( cpnts.length>1) {
            this.branchCode = cpnts[1]
        }
    }

    branchCode: string = ''

    showLinkOnMap(e: any) {
        //
        e.stopPropagation()
        this.dataComponent.showBranchOnMapByCode(this.branchCode)
        //if ( this.node1LocationId == this.node2LocationId ) { 
        //    this.dataComponent.showLocationOnMapById(this.node1LocationId)
        //} else {
        //    this.dataComponent.showBranchOnMap(this.node1LocationId,this.node2LocationId)
        //}
    }

    showBranch(e: any) {
        e.stopPropagation()
        this.dataComponent.showBranch(this.branchCode)
    }


}

