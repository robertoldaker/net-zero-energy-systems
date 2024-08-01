import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DialogAutoCompleteComponent } from './dialog-auto-complete.component';

describe('DialogAutoCompleteComponent', () => {
  let component: DialogAutoCompleteComponent;
  let fixture: ComponentFixture<DialogAutoCompleteComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DialogAutoCompleteComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DialogAutoCompleteComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
