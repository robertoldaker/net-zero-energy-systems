import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowTripTableComponent } from './loadflow-trip-table.component';

describe('LoadflowTripTableComponent', () => {
  let component: LoadflowTripTableComponent;
  let fixture: ComponentFixture<LoadflowTripTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowTripTableComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowTripTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
