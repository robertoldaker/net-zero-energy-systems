import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcDialogComponent } from './boundcalc-dialog.component';

describe('BoundCalcDialogComponent', () => {
  let component: BoundCalcDialogComponent;
  let fixture: ComponentFixture<BoundCalcDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcDialogComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BoundCalcDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
