import { Component} from '@angular/core';
import { Generator, GeneratorType} from 'src/app/data/app.data';
import { LoadflowDataService } from 'src/app/loadflow/loadflow-data-service.service';
import { ColumnDataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../../../datasets/data-table-base/data-table-base.component';
import { SortDirection } from '@angular/material/sort';
import { LoadflowDataComponent } from '../loadflow-data/loadflow-data.component';

@Component({
    selector: 'app-loadflow-data-generators',
    templateUrl: './loadflow-data-generators.component.html',
    styleUrls: ['../loadflow-data-common.css','../../../datasets/data-table-base/data-table-base.component.css','./loadflow-data-generators.component.css']
})
export class LoadflowDataGeneratorsComponent extends DataTableBaseComponent<Generator> {

    constructor(
        private dataService: LoadflowDataService,
        private dialogService: DialogService,
        private dataComponent: LoadflowDataComponent,
     ) {
        super();
        this.dataFilter.sort = { active: 'name', direction: 'asc'};

        //
        this.createDataSource(this.dataService.dataset,dataService.networkData.generators);
        this.displayedColumns = ['buttons','name','type','capacity','scaledGeneration','nodeCount']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.nodeMismatchError = false
            this.createDataSource(this.dataService.dataset,results.generators);
        }))
        //
    }

    generatorTypeEnum = GeneratorType

    edit( e: ICellEditorDataDict) {
        this.dialogService.showLoadflowGeneratorDialog(e);
    }

    add() {
        this.dialogService.showLoadflowGeneratorDialog();
    }

    getMismatchStyle(mm: number | undefined): any {
        if (mm!=undefined && Math.abs(mm) > 0.01) {
            return {'color':'darkred'}
        } else {
            return {};
        }
    }

    getOverallMismatchStyle(): any {
        return this.nodeMismatchError ? {'color':'darkred'} : {}
    }

    filterByGenerator(nodeCode: string) {
        this.dataFilter.reset(true)
        this.dataFilter.searchStr = nodeCode
        this.createDataSource()
    }

    getTLFPercent( element: any) {
        let tlf = element.tlf?.value
        if ( tlf!=undefined ) {
            return (tlf*100).toFixed(0)
        } else {
            return "";
        }
    }


    nodeMismatchError: boolean = false;

}
