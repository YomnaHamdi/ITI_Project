import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { StudentDashboardDto } from '../../../models/story.models';

@Component({
  selector: 'app-progress',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './progress.component.html',
})
export class ProgressComponent implements OnInit {
  private readonly svc    = inject(StoryService);
  private readonly state  = inject(AppStateService);
  private readonly router = inject(Router);

  readonly isLoading = signal(false);
  readonly data      = signal<StudentDashboardDto | null>(null);
  readonly error     = signal<string | null>(null);

  readonly navItems = [
    { icon: '📊', label: 'لوحتي',       route: '/dashboard' },
    { icon: '✏️', label: 'الدروس',      route: '/lessons-list' },
    { icon: '📋', label: 'تقدّمي',       route: '/progress' },
    { icon: '🏆', label: 'إنجازاتي',    route: '/achievements' },
    { icon: '📖', label: 'قصصي',        route: '/my-stories' },
    { icon: '✨', label: 'قصص ذكية',    route: '/ai-story' },
  ];

  ngOnInit(): void {
    const childName = this.state.childName() || this.state.currentUser()?.name || '';
    if (!childName) { this.router.navigate(['/dashboard']); return; }
    this.isLoading.set(true);
    this.svc.getStudentDashboard(childName).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('لم يتم تحميل البيانات.'); }
    });
  }

  logout(): void { this.state.logout(); this.router.navigate(['/auth/login']); }
  scoreColor(s: number): string { return s >= 80 ? '#22C55E' : s >= 50 ? '#F59E0B' : '#EF4444'; }
  formatDate(d: string): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('ar-SA', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
