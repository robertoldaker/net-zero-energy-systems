import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiResultsComponent } from './elsi-results.component';

describe('ElsiResultsComponent', () => {
  let component: ElsiResultsComponent;
  let fixture: ComponentFixture<ElsiResultsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiResultsComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiResultsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
