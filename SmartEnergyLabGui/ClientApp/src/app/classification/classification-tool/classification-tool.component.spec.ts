import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClassificationToolComponent } from './classification-tool.component';

describe('ClassificationToolComponent', () => {
  let component: ClassificationToolComponent;
  let fixture: ComponentFixture<ClassificationToolComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ClassificationToolComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ClassificationToolComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
