import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClassificationToolResultsComponent } from './classification-tool-results.component';

describe('ClassificationToolResultsComponent', () => {
  let component: ClassificationToolResultsComponent;
  let fixture: ComponentFixture<ClassificationToolResultsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ClassificationToolResultsComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ClassificationToolResultsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
