import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcBranchDialogComponent } from './boundcalc-branch-dialog.component';

describe('BoundCalcBranchDialogComponent', () => {
  let component: BoundCalcBranchDialogComponent;
  let fixture: ComponentFixture<BoundCalcBranchDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcBranchDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcBranchDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
