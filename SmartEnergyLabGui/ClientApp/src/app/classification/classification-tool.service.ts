import { EventEmitter, Injectable } from "@angular/core";
import { ClassificationToolInput, ClassificationToolOutput } from "../data/app.data";
import { DataClientService } from "../data/data-client.service";
import { ShowMessageService } from "../main/show-message/show-message.service";

@Injectable({
    providedIn: 'root'
})

export class ClassificationToolService {

    output: ClassificationToolOutput | undefined
    input: ClassificationToolInput | undefined

    constructor(private dataClientService: DataClientService) {
        
    }

    run(input: ClassificationToolInput) {
        this.input = input
        this.dataClientService.RunClassificationTool(input, (output: ClassificationToolOutput) => {
            this.output = output
            this.OutputLoaded.emit(output)
        })
    }

    OutputLoaded = new EventEmitter<ClassificationToolOutput>()

}