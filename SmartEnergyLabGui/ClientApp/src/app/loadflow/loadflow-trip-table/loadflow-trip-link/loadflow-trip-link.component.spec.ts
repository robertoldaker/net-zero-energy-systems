import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowTripLinkComponent } from './loadflow-trip-link.component';

describe('LoadflowTripLinkComponent', () => {
  let component: LoadflowTripLinkComponent;
  let fixture: ComponentFixture<LoadflowTripLinkComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowTripLinkComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowTripLinkComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
