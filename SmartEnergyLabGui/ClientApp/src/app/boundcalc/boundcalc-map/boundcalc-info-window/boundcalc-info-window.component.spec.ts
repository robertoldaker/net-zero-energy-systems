import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcInfoWindowComponent } from './boundcalc-info-window.component';

describe('BoundCalcInfoWindowBaseComponent', () => {
  let component: BoundCalcInfoWindowComponent;
  let fixture: ComponentFixture<BoundCalcInfoWindowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcInfoWindowComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcInfoWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
