import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ClassificationToolDialogComponent } from './classification-tool-dialog.component';

describe('ClassificationToolDialogComponent', () => {
  let component: ClassificationToolDialogComponent;
  let fixture: ComponentFixture<ClassificationToolDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ClassificationToolDialogComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ClassificationToolDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
