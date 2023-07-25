import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowLocInfoWindowComponent } from './loadflow-loc-info-window.component';

describe('LoadflowLocInfoWindowComponent', () => {
  let component: LoadflowLocInfoWindowComponent;
  let fixture: ComponentFixture<LoadflowLocInfoWindowComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [LoadflowLocInfoWindowComponent]
    });
    fixture = TestBed.createComponent(LoadflowLocInfoWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
