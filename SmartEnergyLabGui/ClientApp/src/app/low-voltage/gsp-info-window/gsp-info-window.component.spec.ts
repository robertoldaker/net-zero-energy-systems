import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GspInfoWindowComponent } from './gsp-info-window.component';

describe('GspInfoWindowComponent', () => {
  let component: GspInfoWindowComponent;
  let fixture: ComponentFixture<GspInfoWindowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ GspInfoWindowComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GspInfoWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
