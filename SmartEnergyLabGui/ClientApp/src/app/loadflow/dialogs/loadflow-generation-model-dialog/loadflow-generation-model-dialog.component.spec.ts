import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowGenerationModelDialogComponent } from './loadflow-generation-model-dialog.component';

describe('LoadflowGenerationModelDialogComponent', () => {
  let component: LoadflowGenerationModelDialogComponent;
  let fixture: ComponentFixture<LoadflowGenerationModelDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowGenerationModelDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowGenerationModelDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
