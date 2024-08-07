import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DivAutoScrollerComponent } from './div-auto-scroller.component';

describe('DivAutoScrollerComponent', () => {
  let component: DivAutoScrollerComponent;
  let fixture: ComponentFixture<DivAutoScrollerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DivAutoScrollerComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DivAutoScrollerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
