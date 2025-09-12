import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcStagesComponent } from './boundcalc-stages.component';

describe('BoundCalcStagesComponent', () => {
  let component: BoundCalcStagesComponent;
  let fixture: ComponentFixture<BoundCalcStagesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcStagesComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BoundCalcStagesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
