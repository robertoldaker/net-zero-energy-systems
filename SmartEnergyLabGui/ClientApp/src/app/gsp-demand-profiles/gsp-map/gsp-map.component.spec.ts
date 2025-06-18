import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GspMapComponent } from './gsp-map.component';

describe('GspMapComponent', () => {
  let component: GspMapComponent;
  let fixture: ComponentFixture<GspMapComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ GspMapComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GspMapComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
