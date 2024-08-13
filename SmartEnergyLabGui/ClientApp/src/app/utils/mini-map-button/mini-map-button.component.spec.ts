import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MiniMapButtonComponent } from './mini-map-button.component';

describe('MiniMapButtonComponent', () => {
  let component: MiniMapButtonComponent;
  let fixture: ComponentFixture<MiniMapButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ MiniMapButtonComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MiniMapButtonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
