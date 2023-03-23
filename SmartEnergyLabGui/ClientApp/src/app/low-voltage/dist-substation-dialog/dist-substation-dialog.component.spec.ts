import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DistSubstationDialogComponent } from './dist-substation-dialog.component';

describe('DistSubstationDialogComponent', () => {
  let component: DistSubstationDialogComponent;
  let fixture: ComponentFixture<DistSubstationDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DistSubstationDialogComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(DistSubstationDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
