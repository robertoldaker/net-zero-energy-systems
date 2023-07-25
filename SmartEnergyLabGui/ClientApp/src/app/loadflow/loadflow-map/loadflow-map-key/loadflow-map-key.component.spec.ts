import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowMapKeyComponent } from './loadflow-map-key.component';

describe('LoadflowMapKeyComponent', () => {
  let component: LoadflowMapKeyComponent;
  let fixture: ComponentFixture<LoadflowMapKeyComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [LoadflowMapKeyComponent]
    });
    fixture = TestBed.createComponent(LoadflowMapKeyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
