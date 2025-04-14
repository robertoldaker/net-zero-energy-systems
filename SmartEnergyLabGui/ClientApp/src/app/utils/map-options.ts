
export interface IMapData<T,U> {
    id: number
    options: T
    data: U
}

export class MapOptions<T,U> {
    
    private _optionsArray:IMapData<T,U>[]

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

    add(id: number, options: T, data: U) {
        // push into the array
        this._optionsArray.push({id: id, options: options, data: data})
    }

    getArray():IMapData<T,U>[] {
        return this._optionsArray
    }

    getIndex(id: number): number {
        return this._optionsArray.findIndex(m=>m.id == id)
    }

    get(id: number): IMapData<T,U> | undefined {
        return this._optionsArray.find(m=>m.id == id)
    }
}