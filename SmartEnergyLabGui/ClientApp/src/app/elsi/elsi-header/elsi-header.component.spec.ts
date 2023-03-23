import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiHeaderComponent } from './elsi-header.component';

describe('ElsiHeaderComponent', () => {
  let component: ElsiHeaderComponent;
  let fixture: ComponentFixture<ElsiHeaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiHeaderComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiHeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
