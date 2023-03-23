import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AreaInfoWindowComponent } from './area-info-window.component';

describe('AreaInfoWindowComponent', () => {
  let component: AreaInfoWindowComponent;
  let fixture: ComponentFixture<AreaInfoWindowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AreaInfoWindowComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AreaInfoWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
