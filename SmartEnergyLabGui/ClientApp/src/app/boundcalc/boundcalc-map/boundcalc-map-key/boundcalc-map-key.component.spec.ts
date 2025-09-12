import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcMapKeyComponent } from './boundcalc-map-key.component';

describe('BoundCalcMapKeyComponent', () => {
  let component: BoundCalcMapKeyComponent;
  let fixture: ComponentFixture<BoundCalcMapKeyComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [BoundCalcMapKeyComponent]
    });
    fixture = TestBed.createComponent(BoundCalcMapKeyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
