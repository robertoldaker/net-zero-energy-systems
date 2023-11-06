import { Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})

export class MainService {

    constructor() { 
        this.version = "1.0.13";
    }

    version: string 
}
