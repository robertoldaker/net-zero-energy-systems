import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowDataCtrlsComponent } from './loadflow-data-ctrls.component';

describe('LoadflowDataCtrlsComponent', () => {
  let component: LoadflowDataCtrlsComponent;
  let fixture: ComponentFixture<LoadflowDataCtrlsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowDataCtrlsComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LoadflowDataCtrlsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
