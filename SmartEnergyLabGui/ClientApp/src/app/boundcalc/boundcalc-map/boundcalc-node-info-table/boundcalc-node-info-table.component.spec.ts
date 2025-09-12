import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcNodeInfoTableComponent } from './boundcalc-node-info-table.component';

describe('BoundCalcNodeInfoTableComponent', () => {
  let component: BoundCalcNodeInfoTableComponent;
  let fixture: ComponentFixture<BoundCalcNodeInfoTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcNodeInfoTableComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcNodeInfoTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
