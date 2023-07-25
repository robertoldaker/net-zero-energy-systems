export class MapOptions<T> {
    
    private _optionsArray:{ id: number, options: T}[]
    private _optionsMap:Map<number,number>

    constructor() {
        this._optionsArray = []
        this._optionsMap = new Map()
    }

    clear() {
        this._optionsArray = []
        this._optionsMap.clear()
    }

    add(id: number, options: T) {
        // push into the array
        this._optionsArray.push({id: id, options: options})
        // record in a map the index
        this._optionsMap.set(id,this._optionsArray.length-1)
    }

    getArray():{ id: number, options: T}[] {
        return this._optionsArray
    }

    getIndex(id: number): number {
        let index = this._optionsMap.get(id)
        if ( index==undefined ) {
            return -1
        } else {
            return index
        }
    }
}