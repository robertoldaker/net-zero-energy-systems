import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TransDataComponent } from './trans-data.component';

describe('TransDataComponent', () => {
  let component: TransDataComponent;
  let fixture: ComponentFixture<TransDataComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ TransDataComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TransDataComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
