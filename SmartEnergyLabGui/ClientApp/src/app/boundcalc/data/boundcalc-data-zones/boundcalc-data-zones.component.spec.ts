import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcDataZonesComponent } from './boundcalc-data-zones.component';

describe('BoundCalcDataZonesComponent', () => {
  let component: BoundCalcDataZonesComponent;
  let fixture: ComponentFixture<BoundCalcDataZonesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcDataZonesComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcDataZonesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
