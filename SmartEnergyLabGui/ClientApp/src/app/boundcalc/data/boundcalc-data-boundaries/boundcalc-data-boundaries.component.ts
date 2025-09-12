import { Component } from '@angular/core';
import { Boundary } from '../../../data/app.data';
import { BoundCalcDataService } from '../../boundcalc-data-service.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../../../datasets/data-table-base/data-table-base.component';

@Component({
    selector: 'app-boundcalc-data-boundaries',
    templateUrl: './boundcalc-data-boundaries.component.html',
    styleUrls: ['../boundcalc-data-common.css','../../../datasets/data-table-base/data-table-base.component.css','./boundcalc-data-boundaries.component.css']
})
export class BoundCalcDataBoundariesComponent extends DataTableBaseComponent<Boundary> {

    constructor(private dataService: BoundCalcDataService, private dialogService: DialogService) {
        super();
        this.dataFilter.sort = { active: 'code', direction: 'asc'};
        this.createDataSource(dataService.dataset,this.dataService.networkData.boundaries);
        this.displayedColumns = ['buttons','code','zone']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.createDataSource(this.dataService.dataset,results.boundaries);
        }))
    }
    
    typeName: string = 'Boundary'

    edit( e: ICellEditorDataDict) {
        this.dialogService.showBoundCalcBoundaryDialog(e);
    }

    add() {
        this.dialogService.showBoundCalcBoundaryDialog();
    }

}
