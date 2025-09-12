import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcTripLinkComponent } from './boundcalc-trip-link.component';

describe('BoundCalcTripLinkComponent', () => {
  let component: BoundCalcTripLinkComponent;
  let fixture: ComponentFixture<BoundCalcTripLinkComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcTripLinkComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcTripLinkComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
