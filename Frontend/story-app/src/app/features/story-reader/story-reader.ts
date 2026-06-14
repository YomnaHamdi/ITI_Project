import {
  Component, signal, computed, inject, OnInit, OnDestroy, HostListener
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { StoryService } from '../../services/story';
import { AppStateService } from '../../services/app-state-service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-story-reader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './story-reader.html',
  styleUrl: './story-reader.css'
})
export class StoryReaderComponent implements OnInit, OnDestroy {
  private readonly router       = inject(Router);
  private readonly route        = inject(ActivatedRoute);
  private readonly storyService = inject(StoryService);
  private readonly state        = inject(AppStateService);

  readonly isLoading   = signal(false);
  readonly story       = signal<any>(null);
  readonly pageNum     = signal(1);
  readonly isPlaying   = signal(false);
  readonly imageLoaded = signal(false);

  private utterance: SpeechSynthesisUtterance | null = null;

  readonly activePage = computed(() => {
    const s = this.story();
    if (!s?.pages) return null;
    return s.pages.find((p: any) => p.pageNumber === this.pageNum()) ?? null;
  });
  readonly totalPages = computed(() => this.story()?.pages?.length ?? 0);
  readonly isFirst    = computed(() => this.pageNum() === 1);
  readonly isLast     = computed(() => this.pageNum() === this.totalPages());
  readonly dots       = computed(() =>
    Array.from({ length: this.totalPages() }, (_, i) => i + 1)
  );

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.router.navigate(['/levels']); return; }
    this.loadStory(id);
  }

  ngOnDestroy(): void { this.stopAudio(); }

  @HostListener('window:keydown', ['$event'])
  onKey(e: KeyboardEvent): void {
    const tag = (e.target as HTMLElement)?.tagName;
    if (['INPUT', 'TEXTAREA'].includes(tag)) return;
    if (e.key === 'ArrowRight') this.prev();
    if (e.key === 'ArrowLeft')  this.next();
    if (e.key === ' ') { e.preventDefault(); this.playAudio(); }
  }

  private loadStory(id: string): void {
    const cached = this.state.currentStory();
    if (cached && cached.id === id) { this.story.set(cached); return; }
    this.isLoading.set(true);
    this.storyService.getStory(id).subscribe({
      next: s => {
        this.story.set(s);
        this.state.setStory(s);
        this.isLoading.set(false);
      },
      error: () => { this.isLoading.set(false); this.router.navigate(['/levels']); }
    });
  }

  prev(): void {
    if (this.isFirst()) return;
    this.stopAudio();
    this.imageLoaded.set(false);
    this.pageNum.update(p => p - 1);
  }

  next(): void {
    if (this.isLast()) { this.router.navigate(['/dashboard']); return; }
    this.stopAudio();
    this.imageLoaded.set(false);
    this.pageNum.update(p => p + 1);
  }

  playAudio(): void {
    const page = this.activePage();
    if (!page) return;
    if (this.isPlaying()) { this.stopAudio(); return; }
    if (typeof window === 'undefined' || !('speechSynthesis' in window)) return;
    window.speechSynthesis.cancel();
    this.utterance = new SpeechSynthesisUtterance(page.sentence);
    this.utterance.lang = 'ar-SA';
    this.utterance.rate = 0.8;
    this.utterance.onstart = () => this.isPlaying.set(true);
    this.utterance.onend   = () => this.isPlaying.set(false);
    window.speechSynthesis.speak(this.utterance);
  }

  stopAudio(): void {
    if (typeof window !== 'undefined' && 'speechSynthesis' in window)
      window.speechSynthesis.cancel();
    this.isPlaying.set(false);
  }

  imageUrl(url: string): string {
    if (!url) return '';
    return url.startsWith('http') ? url : `${environment.apiUrl}${url}`;
  }

  onImgLoad(): void { this.imageLoaded.set(true); }
  goBack(): void { this.router.navigate(['/levels']); }
}
