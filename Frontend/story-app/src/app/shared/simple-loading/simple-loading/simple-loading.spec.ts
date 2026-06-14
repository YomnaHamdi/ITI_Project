import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SimpleLoading } from './simple-loading';

describe('SimpleLoading', () => {
  let component: SimpleLoading;
  let fixture: ComponentFixture<SimpleLoading>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SimpleLoading]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SimpleLoading);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
