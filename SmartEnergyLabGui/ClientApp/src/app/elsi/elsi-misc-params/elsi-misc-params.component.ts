import { Component, OnInit } from '@angular/core';
import { DatasetData, ElsiMiscParams } from 'src/app/data/app.data';
import { CellEditorData, DataFilter, ICellEditorDataDict} from 'src/app/datasets/cell-editor/cell-editor.component';
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
            this.rowData = this.createCellDataObjects(this.service.datasetInfo.miscParamsInfo)
        } else {
            this.rowData = this.createCellDataObjects({data:[],userEdits: [], tableName: '', deletedData: [] })
        }
        this.addSub( this.service.DatasetInfoChange.subscribe( (ds) => {
            this.rowData = this.createCellDataObjects(ds.miscParamsInfo)
        }))
    }

    ngOnInit(): void {

    }

    private createCellDataObjects(datasetData: DatasetData<ElsiMiscParams>):ICellEditorDataDict {
        let cellData = this.dataFilter.GetCellDataObjects<ElsiMiscParams>(this.service.dataset,datasetData,(item)=>"KEY")  
        return cellData[0]
    }

    rowData: ICellEditorDataDict
    dataFilter: DataFilter = new DataFilter(1)

    getCellData(key: string):CellEditorData | undefined {
        if ( this.rowData && this.rowData[key]) {
            return this.rowData[key];
        } else {
            return undefined;
        }
    }
    
}

