import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadProfilesComponent } from './load-profiles.component';

describe('LoadProfilesComponent', () => {
  let component: LoadProfilesComponent;
  let fixture: ComponentFixture<LoadProfilesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadProfilesComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(LoadProfilesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
