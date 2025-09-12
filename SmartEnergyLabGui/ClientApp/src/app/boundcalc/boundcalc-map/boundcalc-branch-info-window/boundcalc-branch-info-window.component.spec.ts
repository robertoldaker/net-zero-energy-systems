import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcBranchInfoWindowComponent } from './boundcalc-branch-info-window.component';

describe('BoundCalcBranchInfoWindowComponent', () => {
  let component: BoundCalcBranchInfoWindowComponent;
  let fixture: ComponentFixture<BoundCalcBranchInfoWindowComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [BoundCalcBranchInfoWindowComponent]
    });
    fixture = TestBed.createComponent(BoundCalcBranchInfoWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
