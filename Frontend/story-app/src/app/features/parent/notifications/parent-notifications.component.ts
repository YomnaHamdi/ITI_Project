import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { RecentActivityDto } from '../../../models/story.models';

@Component({
  selector: 'app-parent-notifications',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './parent-notifications.component.html',
})
export class ParentNotificationsComponent implements OnInit {
  private readonly svc = inject(StoryService);

  readonly isLoading   = signal(false);
  readonly activities  = signal<RecentActivityDto[]>([]);

  ngOnInit(): void {
    this.isLoading.set(true);
    this.svc.getKnownStudentNames().subscribe({
      next: names => {
        if (names.length === 0) { this.isLoading.set(false); return; }
        this.svc.getParentDashboard(names[0]).subscribe({
          next:  d => { this.activities.set(d.recentActivity ?? []); this.isLoading.set(false); },
          error: () => this.isLoading.set(false)
        });
      },
      error: () => this.isLoading.set(false)
    });
  }

  activityIcon(type: string): string {
    const m: Record<string,string> = { exam: '📝', story: '📖', lesson: '✏️', writing: '🖊️' };
    return m[type] ?? '🔔';
  }

  formatDate(d: string): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('ar-SA', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
}
