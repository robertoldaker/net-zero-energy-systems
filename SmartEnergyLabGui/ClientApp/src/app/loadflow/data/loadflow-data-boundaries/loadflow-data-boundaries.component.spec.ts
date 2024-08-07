import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowDataBoundariesComponent } from './loadflow-data-boundaries.component';

describe('LoadflowDataBoundariesComponent', () => {
  let component: LoadflowDataBoundariesComponent;
  let fixture: ComponentFixture<LoadflowDataBoundariesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowDataBoundariesComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowDataBoundariesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
