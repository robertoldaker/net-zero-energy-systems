import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcZoneDialogComponent } from './boundcalc-zone-dialog.component';

describe('BoundCalcZoneDialogComponent', () => {
  let component: BoundCalcZoneDialogComponent;
  let fixture: ComponentFixture<BoundCalcZoneDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcZoneDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcZoneDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
