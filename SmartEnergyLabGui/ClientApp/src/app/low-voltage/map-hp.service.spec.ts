import { TestBed } from '@angular/core/testing';

import { MapHpService } from './map-hp.service';

describe('MapHpService', () => {
  let service: MapHpService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MapHpService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
