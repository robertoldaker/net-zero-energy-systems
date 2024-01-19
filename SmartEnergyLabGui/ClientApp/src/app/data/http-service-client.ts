import { HttpClient, HttpParams } from "@angular/common/http";
import { ShowMessageService } from "../main/show-message/show-message.service";

export class HttpServiceClient {
    constructor( 
        private baseUrl: string,
        private http: HttpClient, 
        private showMessageService: ShowMessageService) {        
    }

    public GetBasicRequest(url: string, onLoad: (resp: any)=>void | undefined) {
        this.http.get(this.baseUrl + url).subscribe(resp => {
            if ( onLoad) {
                onLoad(resp);
            }
        },resp => { 
            this.logErrorMessage(resp);
        })
    }

    public GetRequest<T>(url: string, onLoad: (resp: T)=>void | undefined) {
        this.http.get<T>(this.baseUrl + url).subscribe(resp => {
            if ( onLoad) {
                onLoad(resp);
            }
        },resp => { 
            this.logErrorMessage(resp);
        })
    }

    public GetRequestWithMessage<T>(message: string, url: string, onLoad: (resp: T)=>void | undefined) {
        this.showMessageService.showMessage(message);
        this.http.get<T>(this.baseUrl + url).subscribe(resp => {
            this.showMessageService.clearMessage()
            if ( onLoad) {
                onLoad(resp);
            }
        },resp => { 
            this.showMessageService.clearMessage()
            this.logErrorMessage(resp);
        })
    }

    public PostRequest<T>(url: string, data: T,onOk: (resp: string)=>void | undefined) {
        this.http.post<string>(this.baseUrl + url, data).subscribe(resp => {
            if ( onOk) {
                onOk(resp);
            }
        },resp => { 
            this.logErrorMessage(resp);
        })
    }

    public PostRequestWithParams<T>(url: string, data: T,params: HttpParams, onOk: (resp: string)=>void | undefined) {
        this.http.post<string>(this.baseUrl + url, data, {params: params}).subscribe(resp => {
            if ( onOk) {
                onOk(resp);
            }
        },resp => { 
            this.logErrorMessage(resp);
        })
    }

    public PostRequestWithMessage<T>(message: string, url: string, data: T,onOk: (resp: string)=>void | undefined) {
        this.showMessageService.showMessage(message);
        this.http.post<string>(this.baseUrl + url, data).subscribe(resp => {
            this.showMessageService.clearMessage()
            if ( onOk) {
                onOk(resp);
            }
        },resp => { 
            this.showMessageService.clearMessage()
            this.logErrorMessage(resp);
        })
    }

    public PostDialogRequest<T>(url: string, data: T,onOk: (resp: string)=>void | undefined, onError: (error: any)=> void | undefined) {
        this.http.post<string>(this.baseUrl + url, data).subscribe(resp => {
            if ( onOk) {
                onOk(resp);
            }
        },resp => { 
            if ( onError && resp.status == 422) { 
                onError(resp.error) 
            } else {
                this.logErrorMessage(resp);
            }
        })
    }

    private logErrorMessage(error:any) {
        let message:string = error.message;
        if ( typeof error.error === 'string') {
            message += '\n\n' + error.error;
        }        
        this.showMessageService.showModalErrorMessage(message)
    }


}