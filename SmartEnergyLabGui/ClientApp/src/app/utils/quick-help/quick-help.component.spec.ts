import { ComponentFixture, TestBed } from '@angular/core/testing';

import { QuickHelpComponent } from './quick-help.component';

describe('QuickHelpComponent', () => {
  let component: QuickHelpComponent;
  let fixture: ComponentFixture<QuickHelpComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ QuickHelpComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(QuickHelpComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
