import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TablePaginatorComponent } from './table-paginator.component';

describe('TablePaginatorComponent', () => {
  let component: TablePaginatorComponent;
  let fixture: ComponentFixture<TablePaginatorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ TablePaginatorComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TablePaginatorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
