import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowDialogComponent } from './loadflow-dialog.component';

describe('LoadflowDialogComponent', () => {
  let component: LoadflowDialogComponent;
  let fixture: ComponentFixture<LoadflowDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowDialogComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LoadflowDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
