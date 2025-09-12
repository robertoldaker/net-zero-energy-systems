import { Component, Input, OnInit } from '@angular/core';
import { BoundCalcDataComponent } from '../../data/boundcalc-data/boundcalc-data.component';

@Component({
    selector: 'app-boundcalc-trip-link',
    templateUrl: './boundcalc-trip-link.component.html',
    styleUrls: ['./boundcalc-trip-link.component.css']
})
export class BoundCalcTripLinkComponent implements OnInit {

    constructor(private dataComponent: BoundCalcDataComponent) { }

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

    showBranchOnMap(e: any) {
        //
        e.stopPropagation()
        this.dataComponent.showBranchOnMapByCode(this.branchCode)
    }

    showBranch(e: any) {
        e.stopPropagation()
        this.dataComponent.showBranch(this.branchCode)
    }


}

