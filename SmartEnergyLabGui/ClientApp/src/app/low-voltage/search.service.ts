import { Injectable } from '@angular/core';
import { DistributionSubstation } from '../data/app.data';
import { DataClientService } from '../data/data-client.service';

@Injectable({
    providedIn: 'root'
})
export class SearchService {

    constructor(private dataClientService: DataClientService) {
        this.isShown = false
    }

    isShown:boolean

}
