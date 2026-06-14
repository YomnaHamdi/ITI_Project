import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminSidebarComponent } from '../shared/admin-sidebar.component';
import { StoryService } from '../../../services/story';
import { StoryResponse } from '../../../models/story.models';

@Component({
  selector: 'app-admin-stories',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, AdminSidebarComponent],
  templateUrl: './admin-stories.component.html',
})
export class AdminStoriesComponent implements OnInit {
  private readonly svc = inject(StoryService);

  readonly isLoading  = signal(false);
  readonly stories    = signal<StoryResponse[]>([]);
  readonly searchTerm = signal('');
  readonly filter     = signal<'all' | 'approved' | 'pending'>('all');

  readonly filtered = computed(() => {
    const q  = this.searchTerm().toLowerCase();
    const f  = this.filter();
    return this.stories().filter(s => {
      const matchQ = !q || s.title.toLowerCase().includes(q);
      const matchF = f === 'all' || (f === 'approved' && s.isApproved) || (f === 'pending' && !s.isApproved);
      return matchQ && matchF;
    });
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true);
    this.svc.getAllStories().subscribe({
      next:  s => { this.stories.set(s); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  deleteStory(id: string): void {
    if (!confirm('هل تريد حذف هذه القصة نهائياً؟')) return;
    this.svc.deleteStory(id).subscribe({ next: () => this.load() });
  }

  pageCount(s: StoryResponse): number { return s.pages?.length ?? 0; }
  coverImage(s: StoryResponse): string { return s.pages?.[0]?.imageUrl ?? ''; }

  setSearch(v: string)              { this.searchTerm.set(v); }
  setFilter(v: 'all'|'approved'|'pending') { this.filter.set(v); }
}
