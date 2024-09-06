import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiGenerationComponent } from './elsi-generation.component';

describe('ElsiGenerationComponent', () => {
  let component: ElsiGenerationComponent;
  let fixture: ComponentFixture<ElsiGenerationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiGenerationComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiGenerationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
