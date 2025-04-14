import { Component, Input, OnInit } from '@angular/core';
import { LoadflowDataComponent } from '../../data/loadflow-data/loadflow-data.component';
import { BoundaryTrip } from 'src/app/data/app.data';

@Component({
    selector: 'app-loadflow-trip-cell',
    templateUrl: './loadflow-trip-cell.component.html',
    styleUrls: ['./loadflow-trip-cell.component.css']
})
export class LoadflowTripCellComponent implements OnInit {

    constructor(private dataComponent: LoadflowDataComponent) { }

    ngOnInit(): void {
    }

    @Input()
    trip: BoundaryTrip | null = null



}
