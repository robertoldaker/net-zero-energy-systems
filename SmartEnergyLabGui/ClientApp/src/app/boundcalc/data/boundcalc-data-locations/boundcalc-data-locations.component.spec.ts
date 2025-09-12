import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcDataLocationsComponent } from './boundcalc-data-locations.component';

describe('BoundCalcDataLocationsComponent', () => {
  let component: BoundCalcDataLocationsComponent;
  let fixture: ComponentFixture<BoundCalcDataLocationsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcDataLocationsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcDataLocationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
