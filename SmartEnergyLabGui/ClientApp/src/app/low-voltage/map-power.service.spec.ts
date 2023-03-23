import { TestBed } from '@angular/core/testing';

import { MapPowerService } from './map-power.service';

describe('MapPowerService', () => {
  let service: MapPowerService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MapPowerService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
