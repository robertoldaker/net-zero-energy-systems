import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowHeaderComponent } from './loadflow-header.component';

describe('LoadflowHeaderComponent', () => {
  let component: LoadflowHeaderComponent;
  let fixture: ComponentFixture<LoadflowHeaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowHeaderComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LoadflowHeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
