import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcDataBranchesComponent } from './boundcalc-data-branches.component';

describe('BoundCalcDataBranchesComponent', () => {
  let component: BoundCalcDataBranchesComponent;
  let fixture: ComponentFixture<BoundCalcDataBranchesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcDataBranchesComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BoundCalcDataBranchesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
