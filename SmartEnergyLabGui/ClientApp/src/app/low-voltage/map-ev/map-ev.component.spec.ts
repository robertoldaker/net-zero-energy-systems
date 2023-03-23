import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapEvComponent } from './map-ev.component';

describe('MapEvComponent', () => {
  let component: MapEvComponent;
  let fixture: ComponentFixture<MapEvComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MapEvComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(MapEvComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
