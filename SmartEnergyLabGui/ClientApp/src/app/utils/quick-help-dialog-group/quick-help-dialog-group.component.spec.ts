import { ComponentFixture, TestBed } from '@angular/core/testing';

import { QuickHelpDialogGroupComponent } from './quick-help-dialog-group.component';

describe('QuickHelpDialogGroupComponent', () => {
  let component: QuickHelpDialogGroupComponent;
  let fixture: ComponentFixture<QuickHelpDialogGroupComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ QuickHelpDialogGroupComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(QuickHelpDialogGroupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
