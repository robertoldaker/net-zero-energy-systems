import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcMapSearchComponent } from './boundcalc-map-search.component';

describe('BoundCalcMapSearchComponent', () => {
  let component: BoundCalcMapSearchComponent;
  let fixture: ComponentFixture<BoundCalcMapSearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcMapSearchComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcMapSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
