import { TestBed } from '@angular/core/testing';

import { EvDemandService } from './ev-demand.service';

describe('EvDemandService', () => {
  let service: EvDemandService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(EvDemandService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
