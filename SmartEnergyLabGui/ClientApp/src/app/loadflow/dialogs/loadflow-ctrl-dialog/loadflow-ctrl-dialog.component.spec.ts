import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowCtrlDialogComponent } from './loadflow-ctrl-dialog.component';

describe('LoadflowCtrlDialogComponent', () => {
  let component: LoadflowCtrlDialogComponent;
  let fixture: ComponentFixture<LoadflowCtrlDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowCtrlDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowCtrlDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
