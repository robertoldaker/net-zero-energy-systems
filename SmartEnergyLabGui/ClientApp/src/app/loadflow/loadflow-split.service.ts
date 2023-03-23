import { EventEmitter, Injectable } from '@angular/core';

@Injectable({
    providedIn: 'root'
})
export class LoadflowSplitService {
    constructor() {
        this.splitData = new SplitData()
    }

    updateSplitData(left: number, right: number) {
        this.splitData.left = left
        this.splitData.right = right
        this.SplitChange.emit(this.splitData)
    }

    splitData: SplitData

    SplitChange:EventEmitter<SplitData> = new EventEmitter<SplitData>()
}

export class SplitData {
    constructor() {
        this.left = 0
        this.right = 0
    }
    left: number
    right: number
}
