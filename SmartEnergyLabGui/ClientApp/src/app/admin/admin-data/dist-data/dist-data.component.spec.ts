import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DistDataComponent } from './dist-data.component';

describe('DistDataComponent', () => {
  let component: DistDataComponent;
  let fixture: ComponentFixture<DistDataComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DistDataComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DistDataComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
