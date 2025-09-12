import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcMapButtonsComponent } from './boundcalc-map-buttons.component';

describe('BoundCalcMapButtonsComponent', () => {
  let component: BoundCalcMapButtonsComponent;
  let fixture: ComponentFixture<BoundCalcMapButtonsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcMapButtonsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcMapButtonsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
