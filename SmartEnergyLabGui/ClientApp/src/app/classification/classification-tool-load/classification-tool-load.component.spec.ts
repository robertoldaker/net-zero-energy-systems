import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClassificationToolLoadComponent } from './classification-tool-load.component';

describe('ClassificationToolLoadComponent', () => {
  let component: ClassificationToolLoadComponent;
  let fixture: ComponentFixture<ClassificationToolLoadComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ClassificationToolLoadComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ClassificationToolLoadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
