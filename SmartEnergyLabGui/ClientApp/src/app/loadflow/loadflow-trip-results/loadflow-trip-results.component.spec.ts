import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowTripResultsComponent } from './loadflow-trip-results.component';

describe('LoadflowTripResultsComponent', () => {
  let component: LoadflowTripResultsComponent;
  let fixture: ComponentFixture<LoadflowTripResultsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowTripResultsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowTripResultsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
