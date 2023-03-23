import { Injectable } from "@angular/core";

@Injectable({
    providedIn: 'root'
})

export class ShowMessageService {

    message: string = ""
    modal: boolean = false
    show: boolean = false
    isError: boolean = false;
    constructor( ) {
    }

    private setMessage(message: string, modal: boolean, isError: boolean) {
        this.message = message
        this.modal = modal
        this.isError = isError
        this.show = message.length>0
    }

    showModalErrorMessage(message: string) {
        this.setMessage(message,true,true)
    }

    showModalMessage(message: string) {
        this.setMessage(message,true,false)
    }

    showMessage(message: string) {
        this.setMessage(message,false,false)
    }

    showMessageWithTimeout(message: string) {
        this.setMessage(message,false,false)
        window.setTimeout(()=>{
            this.clearMessage()
        }, 2000)
    }

    showErrorMessage(message: string) {
        this.setMessage(message,false,true)
    }

    clearMessage() {
        this.show = false
        this.modal = false
    }

}