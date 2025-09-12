import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcDataGenerationModelsComponent } from './boundcalc-data-generation-models.component';

describe('BoundCalcDataGenerationModelsComponent', () => {
  let component: BoundCalcDataGenerationModelsComponent;
  let fixture: ComponentFixture<BoundCalcDataGenerationModelsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcDataGenerationModelsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcDataGenerationModelsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
