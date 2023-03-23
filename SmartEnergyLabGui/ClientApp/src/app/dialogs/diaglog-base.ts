import { Component } from "@angular/core";
import { FormControl, FormGroup } from "@angular/forms";
import { ComponentBase } from "../utils/component-base";

@Component({
    selector: 'app-base',
    template: `<p>base works!</p>`,
    styles: []
  })
export class DialogBase extends ComponentBase {


    constructor() {
        super()
        this.form = new FormGroup({})
    }

    form: FormGroup

    protected fillErrors(error: any) {
        for (const key in error) {
            this.form.get(key)?.setErrors({serverError: error[key]});
            this.form.get(key)?.markAsTouched();
        }
    }

    protected addFormControl(name: string):FormControl {
        let fc = new FormControl()
        this.form.addControl(name, fc);
        return fc
    }

    getError(name: string) {
        return this.form.get(name)?.errors?.serverError
    }    

}