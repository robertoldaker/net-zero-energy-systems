import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowBoundaryDialogComponent } from './loadflow-boundary-dialog.component';

describe('LoadflowBoundaryDialogComponent', () => {
  let component: LoadflowBoundaryDialogComponent;
  let fixture: ComponentFixture<LoadflowBoundaryDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowBoundaryDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowBoundaryDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
