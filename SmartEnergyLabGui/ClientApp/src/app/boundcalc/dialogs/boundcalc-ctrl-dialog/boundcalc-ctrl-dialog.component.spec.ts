import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcCtrlDialogComponent } from './boundcalc-ctrl-dialog.component';

describe('BoundCalcCtrlDialogComponent', () => {
  let component: BoundCalcCtrlDialogComponent;
  let fixture: ComponentFixture<BoundCalcCtrlDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcCtrlDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcCtrlDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
