import { Injectable, signal } from '@angular/core';
import { StoryResponse } from '../models/story.models';

@Injectable({ providedIn: 'root' })
export class StoryStateService {
  readonly currentStory = signal<StoryResponse | null>(null);

  // Last story ID — survives soft navigation, not hard refresh
  private _lastId: string | null = null;

  setStory(story: StoryResponse): void {
    this._lastId = story.id;
    this.currentStory.set(story);
  }

  clearStory(): void {
    this._lastId = null;
    this.currentStory.set(null);
  }

  get lastStoryId(): string | null {
    return this._lastId;
  }

  hasStory(): boolean {
    return this.currentStory() !== null;
  }
}
