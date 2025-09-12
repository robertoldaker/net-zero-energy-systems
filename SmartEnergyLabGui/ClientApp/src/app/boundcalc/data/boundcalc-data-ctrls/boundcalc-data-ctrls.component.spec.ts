import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcDataCtrlsComponent } from './boundcalc-data-ctrls.component';

describe('BoundCalcDataCtrlsComponent', () => {
  let component: BoundCalcDataCtrlsComponent;
  let fixture: ComponentFixture<BoundCalcDataCtrlsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcDataCtrlsComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BoundCalcDataCtrlsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
