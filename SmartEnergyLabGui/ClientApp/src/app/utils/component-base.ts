import { Component, OnDestroy } from "@angular/core";
import { Subscription } from "rxjs";

@Component({
    selector: 'app-base',
    template: `<p>base works!</p>`,
    styles: []
  })
export class ComponentBase implements OnDestroy {

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