import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiDatasetDialogComponent } from './elsi-dataset-dialog.component';

describe('ElsiDatasetDialogComponent', () => {
  let component: ElsiDatasetDialogComponent;
  let fixture: ComponentFixture<ElsiDatasetDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiDatasetDialogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiDatasetDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
