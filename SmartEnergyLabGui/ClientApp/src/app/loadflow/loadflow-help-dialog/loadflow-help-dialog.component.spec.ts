import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowHelpDialogComponent } from './loadflow-help-dialog.component';

describe('LoadflowHelpDialogComponent', () => {
  let component: LoadflowHelpDialogComponent;
  let fixture: ComponentFixture<LoadflowHelpDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowHelpDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowHelpDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
