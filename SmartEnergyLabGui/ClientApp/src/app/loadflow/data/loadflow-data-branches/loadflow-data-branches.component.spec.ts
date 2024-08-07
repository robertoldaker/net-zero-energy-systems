import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowDataBranchesComponent } from './loadflow-data-branches.component';

describe('LoadflowDataBranchesComponent', () => {
  let component: LoadflowDataBranchesComponent;
  let fixture: ComponentFixture<LoadflowDataBranchesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowDataBranchesComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LoadflowDataBranchesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
