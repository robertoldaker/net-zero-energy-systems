import { EventEmitter, Injectable } from "@angular/core";

@Injectable({
    providedIn: 'root'
})
export class QuickHelpService {

    constructor() {

    }

    helpId: string = ""
    xPos: number = 0
    yPos:number = 0
    show: boolean = false
    title: string = ""

    showQuickHelp(helpId: string, title: string, xPos: number, yPos: number) {
        this.title = title
        this.helpId = helpId
        this.xPos = xPos
        this.yPos = yPos
        this.show = true
        this.ShowQuickHelpEvent.emit()
    }

    clearHelp() {
        this.show = false
        this.helpId = ""
    }

    ShowQuickHelpEvent: EventEmitter<any> = new EventEmitter()

}

export class QuickHelpInfo {
    constructor(public id:string,public title:string,public text:string) {
    }
}
