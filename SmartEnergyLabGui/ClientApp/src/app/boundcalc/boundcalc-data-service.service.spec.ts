import { TestBed } from '@angular/core/testing';

import { BoundCalcDataService } from './boundcalc-data-service.service';

describe('BoundCalcDataServiceService', () => {
  let service: BoundCalcDataService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BoundCalcDataService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
