import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowBranchCodeCellComponent } from './loadflow-branch-code-cell.component';

describe('LoadflowBranchCodeCellComponent', () => {
  let component: LoadflowBranchCodeCellComponent;
  let fixture: ComponentFixture<LoadflowBranchCodeCellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowBranchCodeCellComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowBranchCodeCellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
