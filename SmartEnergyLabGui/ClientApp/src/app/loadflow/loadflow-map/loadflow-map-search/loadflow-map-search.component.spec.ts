import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LoadflowMapSearchComponent } from './loadflow-map-search.component';

describe('LoadflowMapSearchComponent', () => {
  let component: LoadflowMapSearchComponent;
  let fixture: ComponentFixture<LoadflowMapSearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ LoadflowMapSearchComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoadflowMapSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
