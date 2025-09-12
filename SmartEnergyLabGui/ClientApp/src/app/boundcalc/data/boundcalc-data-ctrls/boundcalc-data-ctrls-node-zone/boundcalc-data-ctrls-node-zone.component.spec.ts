import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcDataCtrlsNodeZoneComponent } from './boundcalc-data-ctrls-node-zone.component';

describe('BoundCalcDataCtrlsNodeZoneComponent', () => {
  let component: BoundCalcDataCtrlsNodeZoneComponent;
  let fixture: ComponentFixture<BoundCalcDataCtrlsNodeZoneComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcDataCtrlsNodeZoneComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcDataCtrlsNodeZoneComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
