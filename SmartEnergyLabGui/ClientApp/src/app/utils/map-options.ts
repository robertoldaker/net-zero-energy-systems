
export interface IMapData<T> {
    id: number
    options: T
}

export class MapOptions<T> {
    
    private _optionsArray:IMapData<T>[]

    constructor() {
        this._optionsArray = []
    }

    clear() {
        this._optionsArray = []
    }

    remove(id: number) {
        let index = this.getIndex(id);
        this._optionsArray.splice(index,1);
    }

    add(id: number, options: T) {
        // push into the array
        this._optionsArray.push({id: id, options: options})
    }

    getArray():IMapData<T>[] {
        return this._optionsArray
    }

    getIndex(id: number): number {
        return this._optionsArray.findIndex(m=>m.id == id)
    }

    get(id: number): IMapData<T> | undefined {
        return this._optionsArray.find(m=>m.id == id)
    }
}