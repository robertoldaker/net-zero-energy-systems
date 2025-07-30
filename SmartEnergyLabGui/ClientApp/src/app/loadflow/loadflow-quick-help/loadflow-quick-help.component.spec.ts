import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowQuickHelpComponent } from './loadflow-quick-help.component';

describe('LoadflowQuickHelpComponent', () => {
  let component: LoadflowQuickHelpComponent;
  let fixture: ComponentFixture<LoadflowQuickHelpComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowQuickHelpComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowQuickHelpComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
