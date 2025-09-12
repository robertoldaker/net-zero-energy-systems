import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcHeaderComponent } from './boundcalc-header.component';

describe('BoundCalcHeaderComponent', () => {
  let component: BoundCalcHeaderComponent;
  let fixture: ComponentFixture<BoundCalcHeaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcHeaderComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BoundCalcHeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
