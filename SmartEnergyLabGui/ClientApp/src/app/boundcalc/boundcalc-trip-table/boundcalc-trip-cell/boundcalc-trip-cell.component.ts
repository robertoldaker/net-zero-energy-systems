import { Component, Input, OnInit } from '@angular/core';
import { BoundCalcDataComponent } from '../../data/boundcalc-data/boundcalc-data.component';
import { BoundaryTrip } from 'src/app/data/app.data';

@Component({
    selector: 'app-boundcalc-trip-cell',
    templateUrl: './boundcalc-trip-cell.component.html',
    styleUrls: ['./boundcalc-trip-cell.component.css']
})
export class BoundCalcTripCellComponent implements OnInit {

    constructor(private dataComponent: BoundCalcDataComponent) { }

    ngOnInit(): void {
    }

    @Input()
    trip: BoundaryTrip | null = null



}
