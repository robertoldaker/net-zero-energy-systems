import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiGenParametersComponent } from './elsi-gen-parameters.component';

describe('ElsiGenParametersComponent', () => {
  let component: ElsiGenParametersComponent;
  let fixture: ComponentFixture<ElsiGenParametersComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiGenParametersComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiGenParametersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
