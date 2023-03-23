import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowDataComponent } from './loadflow-data.component';

describe('LoadflowDataComponent', () => {
  let component: LoadflowDataComponent;
  let fixture: ComponentFixture<LoadflowDataComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowDataComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LoadflowDataComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
