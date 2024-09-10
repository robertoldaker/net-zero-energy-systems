import { Component } from '@angular/core';
import { Boundary } from '../../../data/app.data';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../../../datasets/data-table-base/data-table-base.component';

@Component({
    selector: 'app-loadflow-data-boundaries',
    templateUrl: './loadflow-data-boundaries.component.html',
    styleUrls: ['../loadflow-data-common.css','../../../datasets/data-table-base/data-table-base.component.css','./loadflow-data-boundaries.component.css']
})
export class LoadflowDataBoundariesComponent extends DataTableBaseComponent<Boundary> {

    constructor(private dataService: LoadflowDataService, private dialogService: DialogService) {
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
        this.dialogService.showLoadflowBoundaryDialog(e);
    }

    add() {
        this.dialogService.showLoadflowBoundaryDialog();
    }

}
