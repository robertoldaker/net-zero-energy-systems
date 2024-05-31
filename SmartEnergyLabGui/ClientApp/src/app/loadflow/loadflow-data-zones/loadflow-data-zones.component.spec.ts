import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowDataZonesComponent } from './loadflow-data-zones.component';

describe('LoadflowDataZonesComponent', () => {
  let component: LoadflowDataZonesComponent;
  let fixture: ComponentFixture<LoadflowDataZonesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowDataZonesComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowDataZonesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
