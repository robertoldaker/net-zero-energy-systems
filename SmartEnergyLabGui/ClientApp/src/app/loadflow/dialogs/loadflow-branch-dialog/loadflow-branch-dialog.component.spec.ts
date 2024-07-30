import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowBranchDialogComponent } from './loadflow-branch-dialog.component';

describe('LoadflowBranchDialogComponent', () => {
  let component: LoadflowBranchDialogComponent;
  let fixture: ComponentFixture<LoadflowBranchDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowBranchDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowBranchDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
