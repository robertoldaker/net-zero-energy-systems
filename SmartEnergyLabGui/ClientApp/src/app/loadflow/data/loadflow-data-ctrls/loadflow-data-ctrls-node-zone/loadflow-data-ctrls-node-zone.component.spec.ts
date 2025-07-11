import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowDataCtrlsNodeZoneComponent } from './loadflow-data-ctrls-node-zone.component';

describe('LoadflowDataCtrlsNodeZoneComponent', () => {
  let component: LoadflowDataCtrlsNodeZoneComponent;
  let fixture: ComponentFixture<LoadflowDataCtrlsNodeZoneComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowDataCtrlsNodeZoneComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowDataCtrlsNodeZoneComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
