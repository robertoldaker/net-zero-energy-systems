import { Component } from "@angular/core";
import { AbstractControl, FormArray, FormControl, FormGroup } from "@angular/forms";
import { ComponentBase } from "../utils/component-base";
import { ICellEditorDataDict } from "../datasets/cell-editor/cell-editor.component";
import { UserEdit } from "../data/app.data";

@Component({
    selector: 'app-base',
    template: `<p>base works!</p>`,
    styleUrls: ['./dialog-base.css']
})
export class DialogBase extends ComponentBase {


    constructor() {
        super()
        this.form = new FormGroup({})
    }

    form: FormGroup
    dialogData: ICellEditorDataDict | undefined

    protected fillErrors(error: any) {
        for (const key in error) {
            this.form.get(key)?.setErrors({ serverError: error[key] });
            this.form.get(key)?.markAsTouched();
        }
    }

    protected addFormControl(name: string, value: any = null): FormControl {
        let fc = new FormControl()
        this.form.addControl(name, fc);
        if (value != null) {
            fc.setValue(value)
        }
        return fc
    }

    getError(name: string) {
        return this.form.get(name)?.errors?.serverError
    }

    getUserEdit(name: string): UserEdit | undefined {
        return this.dialogData ? this.dialogData[name]?.userEdit : undefined
    }

    setValue(key: string, v: any) {
        this.form.get(key)?.setValue(v)
    }

    getValue(key: string): any {
        return this.form.get(key)?.value
    }

    revertToPrevValue(key: string) {
        let ue = this.getUserEdit(key)
        if (ue) {
            this.form.get(key)?.setValue(ue.prevValue)
            this.form.get(key)?.markAsDirty()
        }
    }

    get dialog(): DialogBase  {
        return this
    }

    getUpdatedControls():IFormControlDict {
        let updatedControls: IFormControlDict = {}
        for (const formControlName in this.form.controls) {
            let fc = this.form.controls[formControlName]
            // only include changed controls
            if ( fc.dirty) {
                updatedControls[formControlName] = fc.value
            }
        }
        return updatedControls
    }

}

export interface IFormControlDict {
    [index: string]: any
}
