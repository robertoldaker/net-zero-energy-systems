import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminLogsHomeComponent } from './admin-logs-home.component';

describe('AdminLogsHomeComponent', () => {
  let component: AdminLogsHomeComponent;
  let fixture: ComponentFixture<AdminLogsHomeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AdminLogsHomeComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminLogsHomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
