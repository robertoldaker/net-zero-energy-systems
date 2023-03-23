import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MatInputEditorComponent } from './mat-input-editor.component';

describe('MatInputEditorComponent', () => {
  let component: MatInputEditorComponent;
  let fixture: ComponentFixture<MatInputEditorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MatInputEditorComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MatInputEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
