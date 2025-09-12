import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcLocInfoWindowComponent } from './boundcalc-loc-info-window.component';

describe('BoundCalcLocInfoWindowComponent', () => {
  let component: BoundCalcLocInfoWindowComponent;
  let fixture: ComponentFixture<BoundCalcLocInfoWindowComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [BoundCalcLocInfoWindowComponent]
    });
    fixture = TestBed.createComponent(BoundCalcLocInfoWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
