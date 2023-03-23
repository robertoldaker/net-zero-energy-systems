import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiHelpDialogComponent } from './elsi-help-dialog.component';

describe('ElsiHelpDialogComponent', () => {
  let component: ElsiHelpDialogComponent;
  let fixture: ComponentFixture<ElsiHelpDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiHelpDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiHelpDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
