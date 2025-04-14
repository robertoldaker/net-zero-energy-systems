import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowTripCellComponent } from './loadflow-trip-cell.component';

describe('LoadflowTripCellComponent', () => {
  let component: LoadflowTripCellComponent;
  let fixture: ComponentFixture<LoadflowTripCellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowTripCellComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowTripCellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
