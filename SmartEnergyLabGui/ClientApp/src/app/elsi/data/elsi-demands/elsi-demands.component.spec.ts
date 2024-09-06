import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiDemandsComponent } from './elsi-demands.component';

describe('ElsiDemandsComponent', () => {
  let component: ElsiDemandsComponent;
  let fixture: ComponentFixture<ElsiDemandsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiDemandsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiDemandsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
