import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcHomeComponent } from './boundcalc-home.component';

describe('BoundCalcHomeComponent', () => {
  let component: BoundCalcHomeComponent;
  let fixture: ComponentFixture<BoundCalcHomeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcHomeComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BoundCalcHomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
