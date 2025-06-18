import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GspHeaderComponent } from './gsp-header.component';

describe('GspHeaderComponent', () => {
  let component: GspHeaderComponent;
  let fixture: ComponentFixture<GspHeaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ GspHeaderComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GspHeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
