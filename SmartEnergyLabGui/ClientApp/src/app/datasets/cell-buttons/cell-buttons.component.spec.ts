import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CellButtonsComponent } from './cell-buttons.component';

describe('CellButtonsComponent', () => {
  let component: CellButtonsComponent;
  let fixture: ComponentFixture<CellButtonsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CellButtonsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CellButtonsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
