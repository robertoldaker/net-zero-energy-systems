import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiLogComponent } from './elsi-log.component';

describe('ElsiLogComponent', () => {
  let component: ElsiLogComponent;
  let fixture: ComponentFixture<ElsiLogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiLogComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiLogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
