import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiMiscParamsComponent } from './elsi-misc-params.component';

describe('ElsiMiscParamsComponent', () => {
  let component: ElsiMiscParamsComponent;
  let fixture: ComponentFixture<ElsiMiscParamsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiMiscParamsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiMiscParamsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
