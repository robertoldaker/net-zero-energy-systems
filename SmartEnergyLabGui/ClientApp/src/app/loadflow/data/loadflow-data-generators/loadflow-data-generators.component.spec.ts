import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowDataGeneratorsComponent } from './loadflow-data-generators.component';

describe('LoadflowDataGeneratorsComponent', () => {
  let component: LoadflowDataGeneratorsComponent;
  let fixture: ComponentFixture<LoadflowDataGeneratorsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowDataGeneratorsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowDataGeneratorsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
