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
    toolRunning: boolean = false

    constructor(private dataClientService: DataClientService) {

    }

    run(input: ClassificationToolInput) {
        this.input = input
        this.toolRunning = true;
        this.dataClientService.RunClassificationTool(input,
            (output: ClassificationToolOutput) => {
                this.output = output
                this.OutputLoaded.emit(output)
            },
            undefined,
            () => {
                this.toolRunning = false;
            }
        )
    }

    OutputLoaded = new EventEmitter<ClassificationToolOutput>()

}
