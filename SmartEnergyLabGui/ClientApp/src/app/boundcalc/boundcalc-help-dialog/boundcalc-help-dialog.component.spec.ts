import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcHelpDialogComponent } from './boundcalc-help-dialog.component';

describe('BoundCalcHelpDialogComponent', () => {
  let component: BoundCalcHelpDialogComponent;
  let fixture: ComponentFixture<BoundCalcHelpDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcHelpDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcHelpDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
