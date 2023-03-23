import { Component, OnInit } from '@angular/core';
import { ElsiMiscParams, TableInfo } from 'src/app/data/app.data';
import { CellEditorData, ICellEditorDataDict } from 'src/app/utils/cell-editor/cell-editor.component';
import { ComponentBase } from 'src/app/utils/component-base';
import { ElsiDataService } from '../elsi-data.service';

@Component({
    selector: 'app-elsi-misc-params',
    templateUrl: './elsi-misc-params.component.html',
    styleUrls: ['./elsi-misc-params.component.css']
})
export class ElsiMiscParamsComponent extends ComponentBase implements OnInit {

    constructor(public service: ElsiDataService) {
        super();
        if ( this.service.datasetInfo) {
            this.cellData = this.createCellDataObjects(this.service.datasetInfo.miscParamsInfo)
        } else {
            this.cellData = this.createCellDataObjects({data:[],userEdits: [], tableName: '' })
        }
        this.addSub( this.service.DatasetInfoChange.subscribe( (ds) => {
            this.cellData = this.createCellDataObjects(ds.miscParamsInfo)
        }))
    }

    ngOnInit(): void {

    }

    private createCellDataObjects(tableInfo: TableInfo<ElsiMiscParams>):ICellEditorDataDict {
        let versionId: number = this.service.dataset ? this.service.dataset.id : 0
        let cellData = CellEditorData.GetCellDataObjects<ElsiMiscParams>(tableInfo,(item)=>"KEY", versionId)  
        return cellData[0]
    }

    cellData: ICellEditorDataDict

    getCellData(key: string):CellEditorData {
        if ( this.cellData && this.cellData[key]) {
            return this.cellData[key];
        } else {
            return new CellEditorData();
        }
    }
    
    get isReadOnly() {
        return this.service.isReadOnly
    }
}
