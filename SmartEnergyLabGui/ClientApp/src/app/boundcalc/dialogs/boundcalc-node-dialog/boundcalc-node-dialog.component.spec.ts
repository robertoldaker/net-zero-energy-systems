import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcNodeDialogComponent } from './boundcalc-node-dialog.component';

describe('BoundCalcNodeDialogComponent', () => {
  let component: BoundCalcNodeDialogComponent;
  let fixture: ComponentFixture<BoundCalcNodeDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcNodeDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcNodeDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
