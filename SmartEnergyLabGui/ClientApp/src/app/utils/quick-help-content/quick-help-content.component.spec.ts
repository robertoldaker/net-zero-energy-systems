import { ComponentFixture, TestBed } from '@angular/core/testing';

import { QuickHelpContentComponent } from './quick-help-content.component';

describe('QuickHelpContentComponent', () => {
  let component: QuickHelpContentComponent;
  let fixture: ComponentFixture<QuickHelpContentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ QuickHelpContentComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(QuickHelpContentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
