import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { RouterLink, RouterLinkActive, ActivatedRoute } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { RecentActivityDto } from '../../../models/story.models';

@Component({
  selector: 'app-parent-notifications',
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './parent-notifications.component.html',
})
export class ParentNotificationsComponent implements OnInit {
  private readonly svc   = inject(StoryService);
  private readonly route = inject(ActivatedRoute);

  readonly isLoading    = signal(false);
  readonly childNames   = signal<string[]>([]);
  readonly selectedChild = signal<string>('');
  readonly activities   = signal<RecentActivityDto[]>([]);

  ngOnInit(): void {
    const preselect = this.route.snapshot.queryParamMap.get('child') ?? '';
    this.isLoading.set(true);
    this.svc.getKnownStudentNames().subscribe({
      next: names => {
        this.childNames.set(names);
        if (names.length === 0) { this.isLoading.set(false); return; }
        const first = preselect && names.includes(preselect) ? preselect : names[0];
        this.selectChild(first);
      },
      error: () => this.isLoading.set(false)
    });
  }

  selectChild(name: string): void {
    this.selectedChild.set(name);
    this.isLoading.set(true);
    this.svc.getParentDashboard(name).subscribe({
      next:  d => { this.activities.set(d.recentActivity ?? []); this.isLoading.set(false); },
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
