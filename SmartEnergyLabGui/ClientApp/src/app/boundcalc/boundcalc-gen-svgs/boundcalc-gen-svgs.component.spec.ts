import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcGenSvgsComponent } from './boundcalc-gen-svgs.component';

describe('BoundCalcGenSvgsComponent', () => {
  let component: BoundCalcGenSvgsComponent;
  let fixture: ComponentFixture<BoundCalcGenSvgsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcGenSvgsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcGenSvgsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
