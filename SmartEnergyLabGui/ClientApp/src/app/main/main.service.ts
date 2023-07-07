import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})

export class MainService {

    constructor() { 
        this.version = "1.0.11";
    }

    version: string 
}
