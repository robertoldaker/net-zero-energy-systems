import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowLocationDialogComponent } from './loadflow-location-dialog.component';

describe('LoadflowLocationDialogComponent', () => {
  let component: LoadflowLocationDialogComponent;
  let fixture: ComponentFixture<LoadflowLocationDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowLocationDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowLocationDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
