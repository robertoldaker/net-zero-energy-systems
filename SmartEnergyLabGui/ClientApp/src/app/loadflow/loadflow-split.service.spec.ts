import { TestBed } from '@angular/core/testing';

import { LoadflowSplitService } from './loadflow-split.service';

describe('LoadflowSplitService', () => {
  let service: LoadflowSplitService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LoadflowSplitService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
