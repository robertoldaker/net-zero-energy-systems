import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcDataComponent } from './boundcalc-data.component';

describe('BoundCalcDataComponent', () => {
  let component: BoundCalcDataComponent;
  let fixture: ComponentFixture<BoundCalcDataComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcDataComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BoundCalcDataComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
