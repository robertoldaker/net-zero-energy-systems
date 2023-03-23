import { TestBed } from '@angular/core/testing';

import { BackgroundTasksService } from './background-tasks.service';

describe('BackgroundTasksService', () => {
  let service: BackgroundTasksService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BackgroundTasksService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
