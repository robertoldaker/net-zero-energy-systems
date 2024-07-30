import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowZoneDialogComponent } from './loadflow-zone-dialog.component';

describe('LoadflowZoneDialogComponent', () => {
  let component: LoadflowZoneDialogComponent;
  let fixture: ComponentFixture<LoadflowZoneDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowZoneDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowZoneDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
