import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcGenerationModelDialogComponent } from './boundcalc-generation-model-dialog.component';

describe('BoundCalcGenerationModelDialogComponent', () => {
  let component: BoundCalcGenerationModelDialogComponent;
  let fixture: ComponentFixture<BoundCalcGenerationModelDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcGenerationModelDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcGenerationModelDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
