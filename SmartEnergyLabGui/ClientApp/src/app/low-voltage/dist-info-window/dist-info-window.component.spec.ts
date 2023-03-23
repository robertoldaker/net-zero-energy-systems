import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DistInfoWindowComponent } from './dist-info-window.component';

describe('DistInfoWindowComponent', () => {
  let component: DistInfoWindowComponent;
  let fixture: ComponentFixture<DistInfoWindowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DistInfoWindowComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(DistInfoWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
