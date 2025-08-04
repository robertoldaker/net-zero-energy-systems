import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DatasetsQuickHelpComponent } from './datasets-quick-help.component';

describe('DatasetsQuickHelpComponent', () => {
  let component: DatasetsQuickHelpComponent;
  let fixture: ComponentFixture<DatasetsQuickHelpComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DatasetsQuickHelpComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DatasetsQuickHelpComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
