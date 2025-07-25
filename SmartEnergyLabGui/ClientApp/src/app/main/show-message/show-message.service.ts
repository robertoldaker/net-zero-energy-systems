import { Injectable } from "@angular/core";

@Injectable({
    providedIn: 'root'
})

export class ShowMessageService {

    message: string = ""
    modal: boolean = false
    show: boolean = false
    isError: boolean = false
    canClose: boolean = true
    constructor( ) {
    }

    private setMessage(message: string, modal: boolean, isError: boolean, canClose: boolean) {
        this.message = message
        this.modal = modal
        this.isError = isError
        this.canClose = canClose
        this.show = message.length>0
    }

    showModalErrorMessage(message: string,canClose:boolean = true) {
        this.setMessage(message,true,true,canClose)
    }

    showModalMessage(message: string, canClose: boolean = true) {
        this.setMessage(message,true,false, canClose)
    }

    showMessage(message: string, canClose: boolean = true) {
        this.setMessage(message,false,false, canClose)
    }

    showMessageWithTimeout(message: string, canClose: boolean = true) {
        this.setMessage(message,false,false, canClose)
        window.setTimeout(()=>{
            this.clearMessage()
        }, 2000)
    }

    showErrorMessage(message: string, canClose: boolean = true) {
        this.setMessage(message,false,true, canClose)
    }

    clearMessage() {
        this.show = false
        this.modal = false
    }

}
