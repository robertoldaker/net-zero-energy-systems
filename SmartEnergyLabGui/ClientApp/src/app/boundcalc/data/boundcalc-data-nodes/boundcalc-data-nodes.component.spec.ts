import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BoundCalcDataNodesComponent } from './boundcalc-data-nodes.component';

describe('BoundCalcDataNodesComponent', () => {
  let component: BoundCalcDataNodesComponent;
  let fixture: ComponentFixture<BoundCalcDataNodesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ BoundCalcDataNodesComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(BoundCalcDataNodesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
