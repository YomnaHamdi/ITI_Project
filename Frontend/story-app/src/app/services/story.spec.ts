import { TestBed } from '@angular/core/testing';

import { Story } from './story';

describe('Story', () => {
  let service: Story;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Story);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
