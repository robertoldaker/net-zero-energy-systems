import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcBranchCodeCellComponent } from './boundcalc-branch-code-cell.component';

describe('BoundCalcBranchCodeCellComponent', () => {
  let component: BoundCalcBranchCodeCellComponent;
  let fixture: ComponentFixture<BoundCalcBranchCodeCellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcBranchCodeCellComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcBranchCodeCellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
