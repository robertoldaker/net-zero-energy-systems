import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcDataBoundariesComponent } from './boundcalc-data-boundaries.component';

describe('BoundCalcDataBoundariesComponent', () => {
  let component: BoundCalcDataBoundariesComponent;
  let fixture: ComponentFixture<BoundCalcDataBoundariesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcDataBoundariesComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcDataBoundariesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
