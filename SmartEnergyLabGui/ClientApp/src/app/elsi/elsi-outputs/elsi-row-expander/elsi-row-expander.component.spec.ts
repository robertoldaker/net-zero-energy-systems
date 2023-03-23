import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiRowExpanderComponent } from './elsi-row-expander.component';

describe('ElsiRowExpanderComponent', () => {
  let component: ElsiRowExpanderComponent;
  let fixture: ComponentFixture<ElsiRowExpanderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiRowExpanderComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiRowExpanderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
