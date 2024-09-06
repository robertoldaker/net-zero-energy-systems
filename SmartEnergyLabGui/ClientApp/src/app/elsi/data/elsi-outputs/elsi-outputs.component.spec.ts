import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiOutputsComponent } from './elsi-outputs.component';

describe('ElsiOutputsComponent', () => {
  let component: ElsiOutputsComponent;
  let fixture: ComponentFixture<ElsiOutputsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiOutputsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiOutputsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
