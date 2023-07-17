import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowMapComponent } from './loadflow-map.component';

describe('LoadflowMapComponent', () => {
  let component: LoadflowMapComponent;
  let fixture: ComponentFixture<LoadflowMapComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [LoadflowMapComponent]
    });
    fixture = TestBed.createComponent(LoadflowMapComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
