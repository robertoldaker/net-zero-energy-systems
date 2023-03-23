import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClassificationToolInputComponent } from './classification-tool-input.component';

describe('ClassificationToolInputComponent', () => {
  let component: ClassificationToolInputComponent;
  let fixture: ComponentFixture<ClassificationToolInputComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ClassificationToolInputComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ClassificationToolInputComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
