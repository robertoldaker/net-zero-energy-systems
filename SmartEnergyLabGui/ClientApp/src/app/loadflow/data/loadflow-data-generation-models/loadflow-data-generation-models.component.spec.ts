import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowDataGenerationModelsComponent } from './loadflow-data-generation-models.component';

describe('LoadflowDataGenerationModelsComponent', () => {
  let component: LoadflowDataGenerationModelsComponent;
  let fixture: ComponentFixture<LoadflowDataGenerationModelsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowDataGenerationModelsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowDataGenerationModelsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
