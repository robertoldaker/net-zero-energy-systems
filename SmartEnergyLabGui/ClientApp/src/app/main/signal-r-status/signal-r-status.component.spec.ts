import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SignalRStatusComponent } from './signal-r-status.component';

describe('SignalRStatusComponent', () => {
  let component: SignalRStatusComponent;
  let fixture: ComponentFixture<SignalRStatusComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SignalRStatusComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SignalRStatusComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
