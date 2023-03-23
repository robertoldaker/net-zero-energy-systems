import { Injectable, OnDestroy } from "@angular/core";
import { Subscription } from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class ServiceBase implements OnDestroy {

    private subs: Subscription[]

    constructor() {
        this.subs = []
    }

    protected addSub(sub: Subscription) {
        this.subs.push(sub);
    }
    
    ngOnDestroy(): void {
        this.subs.forEach(m=>m.unsubscribe())
    }

}