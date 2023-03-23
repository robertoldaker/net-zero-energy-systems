import { Injectable } from '@angular/core';
import { ConsoleLogger } from '@microsoft/signalr/dist/esm/Utils';
import { NameValuePair } from '../data/app.data';

@Injectable({
    providedIn: 'root'
})
export class UtilsService {

    constructor() { 

    }

    getNameValuePairs(obj: any):NameValuePair[] {

        let nvps:NameValuePair[] = [];
        const keys = Object.keys(obj).filter((v) => isNaN(Number(v)));
        
        keys.forEach((key, index) => {
            nvps.push({name: key, value: obj[key]})
        });
        return nvps;                
    }
}
