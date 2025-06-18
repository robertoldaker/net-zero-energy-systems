import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GspHomeComponent } from './gsp-home.component';

describe('GspHomeComponent', () => {
  let component: GspHomeComponent;
  let fixture: ComponentFixture<GspHomeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ GspHomeComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GspHomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
