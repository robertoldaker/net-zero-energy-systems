import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowHomeComponent } from './loadflow-home.component';

describe('LoadflowHomeComponent', () => {
  let component: LoadflowHomeComponent;
  let fixture: ComponentFixture<LoadflowHomeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowHomeComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LoadflowHomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
