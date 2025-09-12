import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcBoundaryDialogComponent } from './boundcalc-boundary-dialog.component';

describe('BoundCalcBoundaryDialogComponent', () => {
  let component: BoundCalcBoundaryDialogComponent;
  let fixture: ComponentFixture<BoundCalcBoundaryDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcBoundaryDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcBoundaryDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
