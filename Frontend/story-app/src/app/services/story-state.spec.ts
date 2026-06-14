import { TestBed } from '@angular/core/testing';

import { StoryState } from './story-state';

describe('StoryState', () => {
  let service: StoryState;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(StoryState);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
