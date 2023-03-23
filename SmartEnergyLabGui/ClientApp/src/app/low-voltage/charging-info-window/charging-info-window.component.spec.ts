import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChargingInfoWindowComponent } from './charging-info-window.component';

describe('ChargingInfoWindowComponent', () => {
  let component: ChargingInfoWindowComponent;
  let fixture: ComponentFixture<ChargingInfoWindowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ChargingInfoWindowComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(ChargingInfoWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
