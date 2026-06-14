import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StoryGenerator } from './story-generator';

describe('StoryGenerator', () => {
  let component: StoryGenerator;
  let fixture: ComponentFixture<StoryGenerator>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StoryGenerator]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StoryGenerator);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
