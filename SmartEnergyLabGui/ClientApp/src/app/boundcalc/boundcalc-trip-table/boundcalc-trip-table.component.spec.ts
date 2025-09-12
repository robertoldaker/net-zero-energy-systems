import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcTripTableComponent } from './boundcalc-trip-table.component';

describe('BoundCalcTripTableComponent', () => {
  let component: BoundCalcTripTableComponent;
  let fixture: ComponentFixture<BoundCalcTripTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcTripTableComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcTripTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
