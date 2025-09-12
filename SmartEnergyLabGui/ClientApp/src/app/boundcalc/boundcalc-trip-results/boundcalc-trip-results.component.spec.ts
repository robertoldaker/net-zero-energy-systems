import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcTripResultsComponent } from './boundcalc-trip-results.component';

describe('BoundCalcTripResultsComponent', () => {
  let component: BoundCalcTripResultsComponent;
  let fixture: ComponentFixture<BoundCalcTripResultsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcTripResultsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcTripResultsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
