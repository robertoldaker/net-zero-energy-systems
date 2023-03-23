import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClassificationInfoComponent } from './classification-info.component';

describe('ClassificationInfoComponent', () => {
  let component: ClassificationInfoComponent;
  let fixture: ComponentFixture<ClassificationInfoComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ClassificationInfoComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ClassificationInfoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
