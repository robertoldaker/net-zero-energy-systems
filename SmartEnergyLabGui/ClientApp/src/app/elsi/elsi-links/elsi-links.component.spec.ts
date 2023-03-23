import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ElsiLinksComponent } from './elsi-links.component';

describe('ElsiLinksComponent', () => {
  let component: ElsiLinksComponent;
  let fixture: ComponentFixture<ElsiLinksComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ ElsiLinksComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ElsiLinksComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
