import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapPowerComponent } from './map-power.component';

describe('MapPowerComponent', () => {
  let component: MapPowerComponent;
  let fixture: ComponentFixture<MapPowerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MapPowerComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(MapPowerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
