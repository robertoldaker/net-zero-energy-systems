import { Component} from '@angular/core';
import { Node, Generator, GeneratorType} from 'src/app/data/app.data';
import { BoundCalcDataService } from 'src/app/boundcalc/boundcalc-data-service.service';
import { ColumnDataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../../../datasets/data-table-base/data-table-base.component';
import { SortDirection } from '@angular/material/sort';
import { BoundCalcDataComponent } from '../boundcalc-data/boundcalc-data.component';

@Component({
    selector: 'app-boundcalc-data-generators',
    templateUrl: './boundcalc-data-generators.component.html',
    styleUrls: ['../boundcalc-data-common.css','../../../datasets/data-table-base/data-table-base.component.css','./boundcalc-data-generators.component.css']
})
export class BoundCalcDataGeneratorsComponent extends DataTableBaseComponent<Generator> {

    constructor(
        private dataService: BoundCalcDataService,
        private dialogService: DialogService,
        private dataComponent: BoundCalcDataComponent,
     ) {
        super();
        this.dataFilter.sort = { active: 'name', direction: 'asc'};
        // code filter
        const typeNames: string[] = Object.keys(GeneratorType).filter(
            (key) => isNaN(Number(key))
          );
        typeNames.unshift('All')
        this.typeDataFilter = new ColumnDataFilter(this, "type", typeNames)
        this.typeDataFilter.filterFcn = (item, colFilter) => {
            if (colFilter.value == 'All') {
                return true
            }  else {
                return GeneratorType[colFilter.value] == item.type
            }
        }
        this.dataFilter.columnFilterMap.set(this.typeDataFilter.columnName, this.typeDataFilter)
        //
        this.createDataSource(this.dataService.dataset,dataService.networkData.generators);
        this.displayedColumns = ['buttons','icon','name','type','capacity','scaledGeneration','nodeCount']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.nodeMismatchError = false
            this.createDataSource(this.dataService.dataset,results.generators);
        }))
        //
    }

    generatorTypeEnum = GeneratorType

    edit( e: ICellEditorDataDict) {
        this.dialogService.showBoundCalcGeneratorDialog(e);
    }

    add() {
        this.dialogService.showBoundCalcGeneratorDialog();
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

    getNodes(genId: number):Node[] {
        return this.dataService.networkData.nodes.data.filter(m=>m.generators.find(n=>n.id===genId));
    }


    nodeMismatchError: boolean = false;
    typeDataFilter: ColumnDataFilter


}
