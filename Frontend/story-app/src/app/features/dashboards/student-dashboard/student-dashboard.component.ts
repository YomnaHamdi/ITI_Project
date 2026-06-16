import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, DecimalPipe, RouterLink, RouterLinkActive],
  templateUrl: './student-dashboard.component.html',
  styleUrl: './student-dashboard.component.css'
})
export class StudentDashboardComponent implements OnInit {
  private readonly service = inject(StoryService);
  readonly state           = inject(AppStateService);
  private readonly router  = inject(Router);

  readonly isLoading = signal(false);
  readonly data      = signal<any>(null);
  nameInput          = this.state.childName() || '';

  readonly weekDays = ['الإثنين','الثلاثاء','الأربعاء','الخميس','الجمعة','السبت','الأحد'];

  readonly weekActivity   = computed(() => this.data()?.weeklyActivity as number[] ?? [0,0,0,0,0,0,0]);
  readonly maxWeekActivity = computed(() => Math.max(...this.weekActivity(), 1));
  barHeight(v: number): number { return Math.round(v / this.maxWeekActivity() * 100); }

  readonly achievements = computed(() => {
    const d = this.data();
    if (!d) return [];
    return [
      { icon:'🔥', label:`${d.currentStreak ?? 0} أيام متتالية`, earned: (d.currentStreak ?? 0) >= 3 },
      { icon:'📚', label:'دودة كتب',     earned: (d.storiesRead ?? 0) >= 3 },
      { icon:'⭐', label:'أول نجمة',      earned: (d.stars ?? 0) >= 1 },
      { icon:'🚀', label:'القارئ السريع', earned: (d.examsCompleted ?? 0) >= 5 },
      { icon:'🎯', label:'علامة كاملة',   earned: (d.avgScore ?? 0) >= 90 },
      { icon:'🏆', label:'سيد المستوى',   earned: (d.lessonsCompleted ?? 0) >= 10 },
    ];
  });

  readonly navItems = [
    { icon:'📊', label:'لوحتي',         route:'/dashboard' },
    { icon:'✏️', label:'الدروس',        route:'/levels' },
    { icon:'📋', label:'تقدّمي',        route:'/progress' },
    { icon:'🏆', label:'إنجازاتي',      route:'/achievements' },
    { icon:'📖', label:'قصصي',          route:'/my-stories' },
    { icon:'✨', label:'قصص ذكية',      route:'/ai-story' },
    { icon:'📚', label:'دروسي',         route:'/my-lessons' },
    { icon:'🎯', label:'دروس مُعيَّنة', route:'/assigned-lessons' },
  ];

  readonly mobileNavItems = [
    { icon:'📊', label:'لوحتي',  route:'/dashboard' },
    { icon:'✏️', label:'الدروس', route:'/levels' },
    { icon:'📖', label:'قصصي',   route:'/my-stories' },
    { icon:'🎯', label:'مُعيَّن', route:'/assigned-lessons' },
    { icon:'📋', label:'تقدّمي', route:'/progress' },
  ];

  ngOnInit(): void {
    if (this.nameInput) this.load(this.nameInput);
  }

  private mockData(name: string): any {
    return {
      childName: name, stars: 0, storiesRead: 0, lessonsCompleted: 0,
      examsCompleted: 0, avgScore: 0, writingAttempts: 0, writingAccepted: 0,
      writingAcceptanceRate: 0, performanceLevel: 'مبتدئ', currentStreak: 0,
      weeklyActivity: [0,0,0,0,0,0,0], inProgressLessons: [],
      topStories: [], topLessons: [], examHistory: [], recentActivity: []
    };
  }

  load(name: string): void {
    if (!name.trim()) return;
    this.isLoading.set(true);
    this.service.getStudentDashboard(name.trim()).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => { this.data.set(this.mockData(name.trim())); this.isLoading.set(false); }
    });
  }

  openBook(id: string): void { this.router.navigate(['/books', id, 'read']); }

  logout(): void {
    this.state.logout();
    this.router.navigate(['/auth/login']);
  }
}
