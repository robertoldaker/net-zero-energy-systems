import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowNodeInfoTableComponent } from './loadflow-node-info-table.component';

describe('LoadflowNodeInfoTableComponent', () => {
  let component: LoadflowNodeInfoTableComponent;
  let fixture: ComponentFixture<LoadflowNodeInfoTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowNodeInfoTableComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowNodeInfoTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
