import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowNodeDialogComponent } from './loadflow-node-dialog.component';

describe('LoadflowNodeDialogComponent', () => {
  let component: LoadflowNodeDialogComponent;
  let fixture: ComponentFixture<LoadflowNodeDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowNodeDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowNodeDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
