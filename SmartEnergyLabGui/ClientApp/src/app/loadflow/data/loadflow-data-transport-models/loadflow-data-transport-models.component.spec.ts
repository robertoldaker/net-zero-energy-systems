import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowDataTransportModelsComponent } from './loadflow-data-transport-models.component';

describe('LoadflowDataTransportModelsComponent', () => {
  let component: LoadflowDataTransportModelsComponent;
  let fixture: ComponentFixture<LoadflowDataTransportModelsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowDataTransportModelsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowDataTransportModelsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
