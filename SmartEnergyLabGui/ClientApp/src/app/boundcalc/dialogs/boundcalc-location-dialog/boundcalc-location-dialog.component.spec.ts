import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcLocationDialogComponent } from './boundcalc-location-dialog.component';

describe('BoundCalcLocationDialogComponent', () => {
  let component: BoundCalcLocationDialogComponent;
  let fixture: ComponentFixture<BoundCalcLocationDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcLocationDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcLocationDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
