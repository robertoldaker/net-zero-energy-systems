import { Component} from '@angular/core';
import { DatasetData, Node, NodeGenerator, Generator} from 'src/app/data/app.data';
import { BoundCalcDataService } from 'src/app/boundcalc/boundcalc-data-service.service';
import { ColumnDataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../../../datasets/data-table-base/data-table-base.component';
import { SortDirection } from '@angular/material/sort';
import { BoundCalcHomeComponent } from '../../boundcalc-home/boundcalc-home.component';
import { BoundCalcDataComponent } from '../boundcalc-data/boundcalc-data.component';
import { DatasetsService } from 'src/app/datasets/datasets.service';

@Component({
    selector: 'app-boundcalc-data-nodes',
    templateUrl: './boundcalc-data-nodes.component.html',
    styleUrls: ['../boundcalc-data-common.css','../../../datasets/data-table-base/data-table-base.component.css','./boundcalc-data-nodes.component.css']
})
export class BoundCalcDataNodesComponent extends DataTableBaseComponent<Node> {

    constructor(
        private dataService: BoundCalcDataService,
        private dialogService: DialogService,
        private datasetsService: DatasetsService,
     ) {
        super();
        this.dataFilter.sort = { active: 'code', direction: 'asc'};

        // code filter
        this.codeDataFilter = new ColumnDataFilter(this,"code",['All','No location','With location'])
        this.codeDataFilter.filterFcn = (item,colFilter) => {
            if ( colFilter.value == 'All') {
                return true
            } else if ( colFilter.value == 'No location') {
                return !item.location
            } else if ( colFilter.value == 'With location') {
                return item.location
            } else {
                throw "unexpected value for code column filter"
            }
        }
        this.dataFilter.columnFilterMap.set(this.codeDataFilter.columnName, this.codeDataFilter)
        // voltage filter
        this.voltageDataFilter = new ColumnDataFilter(this,"voltage")
        this.dataFilter.columnFilterMap.set(this.voltageDataFilter.columnName, this.voltageDataFilter)
        // zone filter
        this.zoneDataFilter = new ColumnDataFilter(this,"zoneName")
        this.dataFilter.columnFilterMap.set(this.zoneDataFilter.columnName, this.zoneDataFilter)

        this.createDataSource(this.dataService.dataset,dataService.networkData.nodes);
        this.displayedColumns = ['buttons','code','voltage','zoneName','demand','generation','ext','tlf','km','mismatch']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.nodeMismatchError = false
            this.generators = results.generators
            this.createDataSource(this.dataService.dataset,results.nodes);
        }))
        this.addSub( dataService.ResultsLoaded.subscribe( (results) => {
            this.nodeMismatchError = results.nodeMismatchError;
            if ( this.nodeMismatchError) {
                let dir:SortDirection = results.nodeMismatchErrorAsc ? 'asc' : 'desc'
                this.dataFilter.sort = { active: 'mismatch', direction: dir};
            }
            this.createDataSource(this.dataService.dataset,results.nodes);
        }))

    }

    edit( e: ICellEditorDataDict) {
        this.dialogService.showBoundCalcNodeDialog(e);
    }

    add() {
        this.dialogService.showBoundCalcNodeDialog();
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

    filterByNode(nodeCode: string) {
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

    deleteGenerator(nodeId: number, gen: Generator) {
        let node = this.dataService.networkData.nodes.data.find(m=>m.id == nodeId)
        if ( node ) {
            // save data
            let generatorIds = node.generators.filter(m=>m.id!=gen.id).map(m=>m.id)
            let data = { generatorIds: generatorIds };
            this.dataService.saveDialog(node.id, "Node", data, () => {
            }, (errors) => {
                console.log(errors)
            })
        }
    }

    isLocalGenerator(nodeId: number, gen: Generator) {
        let node = this.dataService.networkData.nodes.data.find(m=>m.id == nodeId)
        if ( node ) {
            let genId = gen.id
            let newGen = node.newGenerators.find(m=>m.id == genId) ? true : false
            let delGen = node.deletedGenerators.find(m => m.id == genId) ? true : false
            return newGen || delGen;
        } else {
            throw `Cannot find node with id ${nodeId}`
        }
    }

    unDeleteGenerator(nodeId: number, gen: Generator) {
        let node = this.dataService.networkData.nodes.data.find(m=>m.id == nodeId)
        if ( node ) {
            // save data
            let generatorIds = node.generators.map(m=>m.id)
            generatorIds.push(gen.id)
            let data = { generatorIds: generatorIds };
            this.dataService.saveDialog(node.id, "Node", data, () => {
            }, (errors) => {
                console.log(errors)
            })
        }
    }

    get isEditable():boolean {
        return this.datasetsService.isEditable
    }

    codeDataFilter: ColumnDataFilter
    voltageDataFilter: ColumnDataFilter
    zoneDataFilter: ColumnDataFilter
    nodeMismatchError: boolean = false;
    generators: DatasetData<Generator> | undefined

}


