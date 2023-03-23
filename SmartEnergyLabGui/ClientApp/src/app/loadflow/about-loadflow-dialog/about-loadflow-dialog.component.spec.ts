import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AboutLoadflowDialogComponent } from './about-loadflow-dialog.component';

describe('AboutLoadflowDialogComponent', () => {
  let component: AboutLoadflowDialogComponent;
  let fixture: ComponentFixture<AboutLoadflowDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AboutLoadflowDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AboutLoadflowDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
