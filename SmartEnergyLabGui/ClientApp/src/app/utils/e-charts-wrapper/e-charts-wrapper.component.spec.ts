import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EChartsWrapperComponent } from './e-charts-wrapper.component';

describe('EChartsWrapperComponent', () => {
  let component: EChartsWrapperComponent;
  let fixture: ComponentFixture<EChartsWrapperComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ EChartsWrapperComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(EChartsWrapperComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
