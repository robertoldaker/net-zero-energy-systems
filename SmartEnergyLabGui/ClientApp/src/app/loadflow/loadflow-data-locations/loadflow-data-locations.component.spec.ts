import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowDataLocationsComponent } from './loadflow-data-locations.component';

describe('LoadflowDataLocationsComponent', () => {
  let component: LoadflowDataLocationsComponent;
  let fixture: ComponentFixture<LoadflowDataLocationsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowDataLocationsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowDataLocationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
