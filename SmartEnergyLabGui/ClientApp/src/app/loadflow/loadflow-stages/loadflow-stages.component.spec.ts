import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowStagesComponent } from './loadflow-stages.component';

describe('LoadflowStagesComponent', () => {
  let component: LoadflowStagesComponent;
  let fixture: ComponentFixture<LoadflowStagesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowStagesComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LoadflowStagesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
