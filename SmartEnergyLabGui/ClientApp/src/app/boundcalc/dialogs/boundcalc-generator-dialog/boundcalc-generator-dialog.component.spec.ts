import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcGeneratorDialogComponent } from './boundcalc-generator-dialog.component';

describe('BoundCalcGeneratorDialogComponent', () => {
  let component: BoundCalcGeneratorDialogComponent;
  let fixture: ComponentFixture<BoundCalcGeneratorDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcGeneratorDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcGeneratorDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
