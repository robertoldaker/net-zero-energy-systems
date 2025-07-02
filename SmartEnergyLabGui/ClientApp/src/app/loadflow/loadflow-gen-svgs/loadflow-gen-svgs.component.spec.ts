import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowGenSvgsComponent } from './loadflow-gen-svgs.component';

describe('LoadflowGenSvgsComponent', () => {
  let component: LoadflowGenSvgsComponent;
  let fixture: ComponentFixture<LoadflowGenSvgsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowGenSvgsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowGenSvgsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
