import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GspDemandProfilesComponent } from './gsp-demand-profiles.component';

describe('GspDemandProfilesComponent', () => {
  let component: GspDemandProfilesComponent;
  let fixture: ComponentFixture<GspDemandProfilesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ GspDemandProfilesComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GspDemandProfilesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
