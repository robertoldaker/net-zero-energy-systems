import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiDayControlComponent } from './elsi-day-control.component';

describe('ElsiDayControlComponent', () => {
  let component: ElsiDayControlComponent;
  let fixture: ComponentFixture<ElsiDayControlComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiDayControlComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiDayControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
