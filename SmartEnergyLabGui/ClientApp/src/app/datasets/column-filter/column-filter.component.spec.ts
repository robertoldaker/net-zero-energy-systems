import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ColumnFilterComponent } from './column-filter.component';

describe('ColumnFilterComponent', () => {
  let component: ColumnFilterComponent;
  let fixture: ComponentFixture<ColumnFilterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ColumnFilterComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ColumnFilterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
