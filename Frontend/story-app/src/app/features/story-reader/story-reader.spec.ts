import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StoryReader } from './story-reader';

describe('StoryReader', () => {
  let component: StoryReader;
  let fixture: ComponentFixture<StoryReader>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StoryReader]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StoryReader);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
