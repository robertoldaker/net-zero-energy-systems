import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SolarInstallationsComponent } from './solar-installations.component';

describe('SolarInstallationsComponent', () => {
  let component: SolarInstallationsComponent;
  let fixture: ComponentFixture<SolarInstallationsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SolarInstallationsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SolarInstallationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
