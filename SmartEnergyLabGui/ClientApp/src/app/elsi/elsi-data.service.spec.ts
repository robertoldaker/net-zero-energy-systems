import { TestBed } from '@angular/core/testing';

import { ElsiDataService } from './elsi-data.service';

describe('ElsiDataService', () => {
  let service: ElsiDataService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ElsiDataService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
