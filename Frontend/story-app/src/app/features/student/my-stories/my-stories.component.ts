import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { StoryResponse } from '../../../models/story.models';

@Component({
  selector: 'app-my-stories',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './my-stories.component.html',
})
export class MyStoriesComponent implements OnInit {
  private readonly svc    = inject(StoryService);
  private readonly state  = inject(AppStateService);
  private readonly router = inject(Router);

  readonly isLoading = signal(false);
  readonly stories   = signal<StoryResponse[]>([]);
  readonly error     = signal<string | null>(null);

  readonly navItems = [
    { icon: '📊', label: 'لوحتي',    route: '/dashboard' },
    { icon: '✏️', label: 'الدروس',   route: '/lessons-list' },
    { icon: '📋', label: 'تقدّمي',    route: '/progress' },
    { icon: '🏆', label: 'إنجازاتي', route: '/achievements' },
    { icon: '📖', label: 'قصصي',     route: '/my-stories' },
    { icon: '✨', label: 'قصص ذكية', route: '/ai-story' },
  ];

  readonly childName = signal('');

  ngOnInit(): void {
    const name = this.state.childName() || this.state.currentUser()?.name || '';
    if (!name) { this.router.navigate(['/dashboard']); return; }
    this.childName.set(name);
    this.isLoading.set(true);
    this.svc.getMyStories(name).subscribe({
      next:  s => { this.stories.set(s); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('تعذّر تحميل القصص.'); }
    });
  }

  logout(): void { this.state.logout(); this.router.navigate(['/auth/login']); }
  pageCount(story: StoryResponse): number { return story.pages?.length ?? 0; }
  coverImage(story: StoryResponse): string { return story.pages?.[0]?.imageUrl || ''; }
}
