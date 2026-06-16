import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { StudentDashboardDto } from '../../../models/story.models';

interface Badge {
  icon:    string;
  label:   string;
  desc:    string;
  earned:  boolean;
  color:   string;
}

@Component({
  selector: 'app-achievements',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './achievements.component.html',
})
export class AchievementsComponent implements OnInit {
  private readonly svc    = inject(StoryService);
  private readonly state  = inject(AppStateService);
  private readonly router = inject(Router);

  readonly isLoading = signal(false);
  readonly data      = signal<StudentDashboardDto | null>(null);

  readonly navItems = [
    { icon: '📊', label: 'لوحتي',    route: '/dashboard' },
    { icon: '✏️', label: 'الدروس',   route: '/lessons-list' },
    { icon: '📋', label: 'تقدّمي',    route: '/progress' },
    { icon: '🏆', label: 'إنجازاتي', route: '/achievements' },
    { icon: '📖', label: 'قصصي',     route: '/my-stories' },
    { icon: '✨', label: 'قصص ذكية', route: '/ai-story' },
  ];

  readonly badges = computed<Badge[]>(() => {
    const d = this.data();
    return [
      { icon: '⭐', label: 'أول نجمة',        desc: 'احصل على نجمتك الأولى',       earned: (d?.stars ?? 0) >= 1,             color: '#F59E0B' },
      { icon: '🌟', label: '5 نجوم',           desc: 'اجمع 5 نجوم',                 earned: (d?.stars ?? 0) >= 5,             color: '#EAB308' },
      { icon: '💫', label: '10 نجوم',          desc: 'اجمع 10 نجوم',                earned: (d?.stars ?? 0) >= 10,            color: '#F97316' },
      { icon: '📚', label: 'قارئ نشط',         desc: 'اقرأ قصة واحدة على الأقل',   earned: (d?.storiesRead ?? 0) >= 1,       color: '#8B5CF6' },
      { icon: '📖', label: 'دودة كتب',         desc: 'اقرأ 5 قصص',                 earned: (d?.storiesRead ?? 0) >= 5,       color: '#6D28D9' },
      { icon: '✏️', label: 'طالب مجتهد',       desc: 'أكمل درساً واحداً',          earned: (d?.lessonsCompleted ?? 0) >= 1,  color: '#2563EB' },
      { icon: '🎯', label: 'محترف الاختبارات', desc: 'أكمل 3 اختبارات',            earned: (d?.examsCompleted ?? 0) >= 3,    color: '#059669' },
      { icon: '🏆', label: 'متفوق',            desc: 'احصل على 80% في اختبار',     earned: (d?.avgScore ?? 0) >= 80,         color: '#DC2626' },
      { icon: '🚀', label: 'نجم المستقبل',     desc: 'أكمل 10 دروس',               earned: (d?.lessonsCompleted ?? 0) >= 10, color: '#EC4899' },
    ];
  });

  readonly earnedCount = computed(() => this.badges().filter(b => b.earned).length);

  ngOnInit(): void {
    const childName = this.state.childName() || this.state.currentUser()?.name || '';
    if (!childName) { this.router.navigate(['/dashboard']); return; }
    this.isLoading.set(true);
    this.svc.getStudentDashboard(childName).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: (err) => {
        console.warn('Achievements load failed:', err);
        // Set fallback empty data
        this.data.set({
          name: childName,
          level: 1,
          lessonsCompleted: 0,
          totalLessons: 0,
          percentage: 0,
          currentStreak: 0,
          storiesRead: 0,
          stars: 0,
          examsCompleted: 0,
          avgScore: 0,
          weeklyActivity: [0,0,0,0,0,0,0],
          achievements: [],
          recentActivity: [],
          recentLessons: []
        } as any);
        this.isLoading.set(false);
      }
    });
  }

  logout(): void { this.state.logout(); this.router.navigate(['/auth/login']); }
}
