import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcTripCellComponent } from './boundcalc-trip-cell.component';

describe('BoundCalcTripCellComponent', () => {
  let component: BoundCalcTripCellComponent;
  let fixture: ComponentFixture<BoundCalcTripCellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcTripCellComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcTripCellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
