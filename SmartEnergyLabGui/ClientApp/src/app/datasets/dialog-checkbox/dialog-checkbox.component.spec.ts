import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DialogCheckboxComponent } from './dialog-checkbox.component';

describe('DialogCheckboxComponent', () => {
  let component: DialogCheckboxComponent;
  let fixture: ComponentFixture<DialogCheckboxComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DialogCheckboxComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DialogCheckboxComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
