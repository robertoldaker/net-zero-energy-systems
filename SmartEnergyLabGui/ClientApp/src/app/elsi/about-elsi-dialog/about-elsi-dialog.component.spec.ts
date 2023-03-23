import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AboutElsiDialogComponent } from './about-elsi-dialog.component';

describe('AboutElsiDialogComponent', () => {
  let component: AboutElsiDialogComponent;
  let fixture: ComponentFixture<AboutElsiDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AboutElsiDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AboutElsiDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
