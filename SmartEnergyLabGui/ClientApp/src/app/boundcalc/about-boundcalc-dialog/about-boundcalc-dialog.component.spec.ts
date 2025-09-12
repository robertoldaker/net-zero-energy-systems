import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AboutBoundCalcDialogComponent } from './about-boundcalc-dialog.component';

describe('AboutBoundCalcDialogComponent', () => {
  let component: AboutBoundCalcDialogComponent;
  let fixture: ComponentFixture<AboutBoundCalcDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AboutBoundCalcDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AboutBoundCalcDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
