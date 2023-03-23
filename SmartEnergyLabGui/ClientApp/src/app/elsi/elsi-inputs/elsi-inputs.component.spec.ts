import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiInputsComponent } from './elsi-inputs.component';

describe('ElsiInputsComponent', () => {
  let component: ElsiInputsComponent;
  let fixture: ComponentFixture<ElsiInputsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiInputsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiInputsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
