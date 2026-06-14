import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WritingCorrection } from './writing-correction';

describe('WritingCorrection', () => {
  let component: WritingCorrection;
  let fixture: ComponentFixture<WritingCorrection>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WritingCorrection]
    })
    .compileComponents();

    fixture = TestBed.createComponent(WritingCorrection);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
