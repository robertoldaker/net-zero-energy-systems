import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowTransportModelDialogComponent } from './loadflow-transport-model-dialog.component';

describe('LoadflowTransportModelDialogComponent', () => {
  let component: LoadflowTransportModelDialogComponent;
  let fixture: ComponentFixture<LoadflowTransportModelDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowTransportModelDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowTransportModelDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
