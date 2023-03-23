import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiDialogComponent } from './elsi-dialog.component';

describe('ElsiDialogComponent', () => {
  let component: ElsiDialogComponent;
  let fixture: ComponentFixture<ElsiDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
