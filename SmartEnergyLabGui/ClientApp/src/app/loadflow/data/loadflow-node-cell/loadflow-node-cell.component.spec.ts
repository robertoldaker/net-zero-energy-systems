import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowNodeCellComponent } from './loadflow-node-cell.component';

describe('LoadflowNodeCellComponent', () => {
  let component: LoadflowNodeCellComponent;
  let fixture: ComponentFixture<LoadflowNodeCellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowNodeCellComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowNodeCellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
