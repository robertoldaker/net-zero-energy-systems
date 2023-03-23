import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiGenCapacitiesComponent } from './elsi-gen-capacities.component';

describe('ElsiGenCapacitiesComponent', () => {
  let component: ElsiGenCapacitiesComponent;
  let fixture: ComponentFixture<ElsiGenCapacitiesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiGenCapacitiesComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiGenCapacitiesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
