import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcNodeCellComponent } from './boundcalc-node-cell.component';

describe('BoundCalcNodeCellComponent', () => {
  let component: BoundCalcNodeCellComponent;
  let fixture: ComponentFixture<BoundCalcNodeCellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcNodeCellComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BoundCalcNodeCellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
