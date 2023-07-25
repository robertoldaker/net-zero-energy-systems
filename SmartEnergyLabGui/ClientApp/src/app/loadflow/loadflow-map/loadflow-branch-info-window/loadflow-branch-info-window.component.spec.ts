import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowBranchInfoWindowComponent } from './loadflow-branch-info-window.component';

describe('LoadflowBranchInfoWindowComponent', () => {
  let component: LoadflowBranchInfoWindowComponent;
  let fixture: ComponentFixture<LoadflowBranchInfoWindowComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [LoadflowBranchInfoWindowComponent]
    });
    fixture = TestBed.createComponent(LoadflowBranchInfoWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
