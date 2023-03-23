import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClassificationToolClusterProbabilitiesComponent } from './classification-tool-cluster-probabilities.component';

describe('ClassificationToolClusterProbabilitiesComponent', () => {
  let component: ClassificationToolClusterProbabilitiesComponent;
  let fixture: ComponentFixture<ClassificationToolClusterProbabilitiesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ClassificationToolClusterProbabilitiesComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ClassificationToolClusterProbabilitiesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
