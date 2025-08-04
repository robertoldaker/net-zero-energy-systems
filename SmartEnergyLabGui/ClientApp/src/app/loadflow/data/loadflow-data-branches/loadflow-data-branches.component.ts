import { Component } from '@angular/core';
import { Branch, BranchType } from '../../../data/app.data';
import { LoadflowDataService, PercentCapacityThreshold as FlowCapacityThreshold } from '../../loadflow-data-service.service';
import { ColumnDataFilter, ICellEditorDataDict } from 'src/app/datasets/cell-editor/cell-editor.component';
import { DialogService } from 'src/app/dialogs/dialog.service';
import { DataTableBaseComponent } from '../../../datasets/data-table-base/data-table-base.component';
import { LoadflowDataComponent } from '../loadflow-data/loadflow-data.component';
import { Sort } from '@angular/material/sort';

@Component({
  selector: 'app-loadflow-data-branches',
  templateUrl: './loadflow-data-branches.component.html',
  styleUrls: ['../loadflow-data-common.css','../../../datasets/data-table-base/data-table-base.component.css','./loadflow-data-branches.component.css']
})
export class LoadflowDataBranchesComponent extends DataTableBaseComponent<Branch> {

    constructor(private dataService: LoadflowDataService,
                private dialogService: DialogService,
                private dataComponent: LoadflowDataComponent) {
        super()
        this.dataFilter.sort = { active: 'node1Code', direction: 'asc'};
        this.typeDataFilter = new ColumnDataFilter(this,"typeStr")
        this.dataFilter.columnFilterMap.set(this.typeDataFilter.columnName, this.typeDataFilter)
        this.dataFilter.addCustomSorter("trip",(col: string, item1: any, item2: any)=>{
            let isTripped1 = this.dataService.isTripped(item1.id)
            let isTripped2 = this.dataService.isTripped(item2.id)
            if (isTripped1 == isTripped2 ) {
                return 0
            } else {
                return isTripped1 && !isTripped2 ? 1 : -1
            }
        })

        this.createDataSource(dataService.dataset,dataService.networkData.branches)
        this.displayedColumns = ['buttons','code','node1Code','node2Code','typeStr','ohl','cableLength','x','r','cap','trip','freePower','powerFlow','percentCapacity','km','mwkm','loss']
        this.addSub( dataService.NetworkDataLoaded.subscribe( (results) => {
            this.branchCapacityError = false
            this.createDataSource(dataService.dataset,results.branches)
        }))
        this.addSub(dataService.ResultsLoaded.subscribe( (results) => {
            this.branchCapacityError = results.branchCapacityError;
            if ( this.branchCapacityError) {
                this.dataFilter.sort = { active: 'freePower', direction: 'asc'};
            }
            this.createDataSource(dataService.dataset,results.branches)
        }))
    }

    typeName: string = "Branch"
    typeDataFilter: ColumnDataFilter
    branchCapacityError: boolean = false;

    getTypeStr(type: BranchType) {
        return BranchType[type];
    }

    edit( e: ICellEditorDataDict) {
        let branchId = e._data.id
        let branchEditorData = this.dataService.getBranchEditorData(branchId)
        this.dialogService.showLoadflowBranchDialog(branchEditorData);
    }

    add() {
        this.dialogService.showLoadflowBranchDialog();
    }

    getFreePowerStyle(fp: number | undefined): any {
        if (fp!=undefined && fp<-1e-2 ) {
            return {'color':'darkred'}
        } else {
            return {};
        }
    }

    getOverallFreePowerStyle(): any {
        return this.branchCapacityError ? {'color':'darkred'} : {}
    }

    filterByNode(nodeCode: string) {
        this.dataFilter.reset(true)
        this.dataFilter.searchStr = nodeCode
        this.createDataSource()
    }

    filterByCode(code: string) {
        //
        this.dataFilter.reset(true)
        this.dataFilter.searchStr = code
        this.createDataSource()
    }

    isTripped(branchId: number) {
        return this.dataService.isTripped(branchId)
    }

    get canTrip():boolean {
        return this.dataService.boundaryName ? true : false
    }

    toggleTrip(e: any, branchId: number) {
        if ( e.target.checked ) {
            this.dataService.addTrip(branchId)
        } else {
            this.dataService.removeTrip(branchId)
        }
    }

    clearTrips(e:any) {
        this.dataService.clearTrips()
        e.stopPropagation()
    }

    flowCapStyle(type: BranchType, percentCap: any):any {
        let style = {}
        if ( percentCap ) {
            let threshold = this.dataService.getFlowCapacityThreshold(type, percentCap)
            let color = ''
            if ( threshold == FlowCapacityThreshold.Critical) {
                color = 'darkred'
            } else if(threshold == FlowCapacityThreshold.Warning) {
                color = 'coral'
            }
            if ( color ) {
                style= {color: color};
            }
        }
        return style
    }

}
