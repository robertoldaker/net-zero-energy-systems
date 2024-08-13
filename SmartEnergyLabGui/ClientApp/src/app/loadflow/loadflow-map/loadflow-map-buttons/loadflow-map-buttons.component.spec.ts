import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowMapButtonsComponent } from './loadflow-map-buttons.component';

describe('LoadflowMapButtonsComponent', () => {
  let component: LoadflowMapButtonsComponent;
  let fixture: ComponentFixture<LoadflowMapButtonsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowMapButtonsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowMapButtonsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
