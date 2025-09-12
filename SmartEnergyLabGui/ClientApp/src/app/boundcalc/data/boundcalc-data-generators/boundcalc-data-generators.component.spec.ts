import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcDataGeneratorsComponent } from './boundcalc-data-generators.component';

describe('BoundCalcDataGeneratorsComponent', () => {
  let component: BoundCalcDataGeneratorsComponent;
  let fixture: ComponentFixture<BoundCalcDataGeneratorsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcDataGeneratorsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcDataGeneratorsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
