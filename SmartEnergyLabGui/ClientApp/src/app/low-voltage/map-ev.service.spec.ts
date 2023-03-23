import { TestBed } from '@angular/core/testing';

import { MapEvService } from './map-ev.service';

describe('MapEvService', () => {
  let service: MapEvService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MapEvService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
