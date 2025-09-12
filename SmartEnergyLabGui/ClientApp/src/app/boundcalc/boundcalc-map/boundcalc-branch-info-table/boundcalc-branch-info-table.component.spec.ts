import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcBranchInfoTableComponent } from './boundcalc-branch-info-table.component';

describe('BoundCalcBranchInfoTableComponent', () => {
  let component: BoundCalcBranchInfoTableComponent;
  let fixture: ComponentFixture<BoundCalcBranchInfoTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcBranchInfoTableComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcBranchInfoTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
