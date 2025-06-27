import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowInfoWindowComponent } from './loadflow-info-window.component';

describe('LoadflowInfoWindowBaseComponent', () => {
  let component: LoadflowInfoWindowComponent;
  let fixture: ComponentFixture<LoadflowInfoWindowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowInfoWindowComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowInfoWindowComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
