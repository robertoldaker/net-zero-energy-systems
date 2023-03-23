import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiHomeComponent } from './elsi-home.component';

describe('ElsiHomeComponent', () => {
  let component: ElsiHomeComponent;
  let fixture: ComponentFixture<ElsiHomeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiHomeComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiHomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
