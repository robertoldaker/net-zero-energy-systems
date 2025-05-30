import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowGeneratorDialogComponent } from './loadflow-generator-dialog.component';

describe('LoadflowGeneratorDialogComponent', () => {
  let component: LoadflowGeneratorDialogComponent;
  let fixture: ComponentFixture<LoadflowGeneratorDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowGeneratorDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowGeneratorDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
