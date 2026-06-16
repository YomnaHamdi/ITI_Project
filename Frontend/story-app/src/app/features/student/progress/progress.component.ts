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
  readonly childName = signal('');

  readonly navItems = [
    { icon: '📊', label: 'لوحتي',    route: '/dashboard' },
    { icon: '✏️', label: 'الدروس',   route: '/lessons-list' },
    { icon: '📋', label: 'تقدّمي',    route: '/progress' },
    { icon: '🏆', label: 'إنجازاتي', route: '/achievements' },
    { icon: '📖', label: 'قصصي',     route: '/my-stories' },
    { icon: '✨', label: 'قصص ذكية', route: '/ai-story' },
  ];

  private mockData(name: string): StudentDashboardDto {
    return {
      childName: name, stars: 0, storiesRead: 0, lessonsCompleted: 0,
      examsCompleted: 0, avgScore: 0, writingAttempts: 0, writingAccepted: 0,
      writingAcceptanceRate: 0, performanceLevel: 'مبتدئ', currentStreak: 0,
      weeklyActivity: [0,0,0,0,0,0,0], inProgressLessons: [],
      topStories: [], topLessons: [], examHistory: [], recentActivity: []
    } as StudentDashboardDto;
  }

  ngOnInit(): void {
    const name = this.state.childName() || this.state.currentUser()?.name || 'طالب';
    this.childName.set(name);
    this.isLoading.set(true);
    this.svc.getStudentDashboard(name).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => { this.data.set(this.mockData(name)); this.isLoading.set(false); }
    });
  }

  logout(): void { this.state.logout(); this.router.navigate(['/auth/login']); }

  scoreColor(s: number): string { return s >= 80 ? '#22C55E' : s >= 50 ? '#F59E0B' : '#EF4444'; }

  formatDate(d: string): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('ar-SA', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
