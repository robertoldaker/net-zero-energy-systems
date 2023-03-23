import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PrimaryInfoWindowComponent } from './primary-info-window.component';

describe('PrimaryInfoWindowComponent', () => {
  let component: PrimaryInfoWindowComponent;
  let fixture: ComponentFixture<PrimaryInfoWindowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ PrimaryInfoWindowComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(PrimaryInfoWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
