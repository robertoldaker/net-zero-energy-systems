import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GspSearchComponent } from './gsp-search.component';

describe('GspSearchComponent', () => {
  let component: GspSearchComponent;
  let fixture: ComponentFixture<GspSearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ GspSearchComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GspSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
