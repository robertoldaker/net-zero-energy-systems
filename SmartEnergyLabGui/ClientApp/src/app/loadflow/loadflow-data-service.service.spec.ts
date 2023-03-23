import { TestBed } from '@angular/core/testing';

import { LoadflowDataService } from './loadflow-data-service.service';

describe('LoadflowDataServiceService', () => {
  let service: LoadflowDataService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LoadflowDataService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
