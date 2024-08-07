import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowDataNodesComponent } from './loadflow-data-nodes.component';

describe('LoadflowDataNodesComponent', () => {
  let component: LoadflowDataNodesComponent;
  let fixture: ComponentFixture<LoadflowDataNodesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowDataNodesComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LoadflowDataNodesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
