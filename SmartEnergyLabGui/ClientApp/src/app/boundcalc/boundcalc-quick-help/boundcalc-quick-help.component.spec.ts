import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcQuickHelpComponent } from './boundcalc-quick-help.component';

describe('BoundCalcQuickHelpComponent', () => {
  let component: BoundCalcQuickHelpComponent;
  let fixture: ComponentFixture<BoundCalcQuickHelpComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcQuickHelpComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcQuickHelpComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
