import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapHpComponent } from './map-hp.component';

describe('MapHpComponent', () => {
  let component: MapHpComponent;
  let fixture: ComponentFixture<MapHpComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MapHpComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(MapHpComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
