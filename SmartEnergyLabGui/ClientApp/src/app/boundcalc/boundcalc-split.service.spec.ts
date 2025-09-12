import { TestBed } from '@angular/core/testing';

import { BoundCalcSplitService } from './boundcalc-split.service';

describe('BoundCalcSplitService', () => {
  let service: BoundCalcSplitService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BoundCalcSplitService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
