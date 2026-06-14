import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ErrorToast } from './error-toast';

describe('ErrorToast', () => {
  let component: ErrorToast;
  let fixture: ComponentFixture<ErrorToast>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ErrorToast]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ErrorToast);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
