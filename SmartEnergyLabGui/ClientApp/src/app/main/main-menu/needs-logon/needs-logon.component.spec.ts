import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NeedsLogonComponent } from './needs-logon.component';

describe('NeedsLogonComponent', () => {
  let component: NeedsLogonComponent;
  let fixture: ComponentFixture<NeedsLogonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ NeedsLogonComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NeedsLogonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
