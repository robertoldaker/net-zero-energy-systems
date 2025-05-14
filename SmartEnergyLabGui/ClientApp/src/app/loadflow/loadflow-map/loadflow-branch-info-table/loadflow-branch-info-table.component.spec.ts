import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowBranchInfoTableComponent } from './loadflow-branch-info-table.component';

describe('LoadflowBranchInfoTableComponent', () => {
  let component: LoadflowBranchInfoTableComponent;
  let fixture: ComponentFixture<LoadflowBranchInfoTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowBranchInfoTableComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowBranchInfoTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
