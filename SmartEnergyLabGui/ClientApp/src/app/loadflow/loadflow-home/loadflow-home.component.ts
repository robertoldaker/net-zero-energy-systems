import { AfterContentInit, AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { LoadflowSplitService } from '../loadflow-split.service';

@Component({
    selector: 'app-loadflow-home',
    templateUrl: './loadflow-home.component.html',
    styleUrls: ['./loadflow-home.component.css']
})
export class LoadflowHomeComponent implements OnInit, AfterViewInit{

    constructor(private splitService: LoadflowSplitService) {

    }
    ngAfterViewInit(): void {
        this.updateSplitData()        
    }

    @ViewChild('leftDiv') leftView: ElementRef | undefined;
    @ViewChild('rightDiv') rightView: ElementRef | undefined;

    ngOnInit(): void {
    }

    splitStart() {

    }

    splitEnd(e: any) {
        this.updateSplitData()
    }

    updateSplitData() {
        let lw = this.leftView?.nativeElement.clientWidth
        let rw = this.rightView?.nativeElement.clientWidth
        this.splitService.updateSplitData(lw,rw)
    }

}
