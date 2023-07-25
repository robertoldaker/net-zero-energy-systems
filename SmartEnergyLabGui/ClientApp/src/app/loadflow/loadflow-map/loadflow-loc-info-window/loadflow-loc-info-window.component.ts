import { Component } from '@angular/core';
import { ComponentBase } from 'src/app/utils/component-base';
import { LoadflowDataService } from '../../loadflow-data-service.service';
import { LoadflowLocation } from 'src/app/data/app.data';

@Component({
  selector: 'app-loadflow-loc-info-window',
  templateUrl: './loadflow-loc-info-window.component.html',
  styleUrls: ['./loadflow-loc-info-window.component.css']
})

export class LoadflowLocInfoWindowComponent extends ComponentBase {
    constructor(private loadflowDataService: LoadflowDataService) {
        super()
        this.addSub(this.loadflowDataService.ObjectSelected.subscribe( (selectedMapItem)=>{
            this.loc = selectedMapItem.location
        }))
    }

    loc: LoadflowLocation| null = null
    get name():string {
        return this.loc ? this.loc?.name : ''
    }

    get isQB():boolean {
        return this.loc ? this.loc?.isQB : false
    }

    get fillColor():string {
        return this.isQB ? 'grey' : '#7E4444'
    }

}
