import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcMapComponent } from './boundcalc-map.component';

describe('BoundCalcMapComponent', () => {
  let component: BoundCalcMapComponent;
  let fixture: ComponentFixture<BoundCalcMapComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [BoundCalcMapComponent]
    });
    fixture = TestBed.createComponent(BoundCalcMapComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
