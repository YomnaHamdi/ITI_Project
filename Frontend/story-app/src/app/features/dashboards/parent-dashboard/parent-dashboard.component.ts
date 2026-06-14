import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule, DecimalPipe } from '@angular/common';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';

@Component({
  selector: 'app-parent-dashboard',
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink, NavbarComponent],
  templateUrl: './parent-dashboard.component.html',
  styleUrl: './parent-dashboard.component.css'
})
export class ParentDashboardComponent implements OnInit {
  private readonly service = inject(StoryService);
  private readonly router  = inject(Router);

  readonly isLoading   = signal(false);
  readonly childNames  = signal<string[]>([]);
  readonly activeChild = signal<string>('');
  readonly data        = signal<any>(null);
  readonly error       = signal<string | null>(null);

  readonly weekDays       = ['الاثنين','الثلاثاء','الأربعاء','الخميس','الجمعة','السبت','الأحد'];
  readonly weekActivity   = computed(() => this.data()?.weeklyActivity as number[] ?? [0,0,0,0,0,0,0]);
  readonly maxWeekActivity = computed(() => Math.max(...this.weekActivity(), 1));
  barH(v: number): number { return Math.round(v / this.maxWeekActivity() * 100); }

  readonly achievements = computed(() => {
    const d = this.data();
    if (!d) return [];
    return [
      { icon:'🔥', label:`${d.currentStreak ?? 0} أيام متتالية` },
      { icon:'📚', label:'دودة كتب',  show: (d.storiesRead ?? 0) >= 3 },
      { icon:'⭐', label:'أول نجمة',   show: (d.stars ?? 0) >= 1 },
    ].filter(a => (a as any).show !== false);
  });

  ngOnInit(): void {
    this.service.getKnownStudentNames().subscribe({
      next: names => {
        this.childNames.set(names);
        if (names.length > 0) this.selectChild(names[0]);
      }
    });
  }

  selectChild(name: string): void {
    this.activeChild.set(name);
    this.isLoading.set(true);
    this.error.set(null);
    this.service.getParentDashboard(name).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('لم يتم العثور على بيانات.'); }
    });
  }

  continueLesson(id: string): void { this.router.navigate(['/books', id, 'read']); }
  skillColor(pct: number): string { return pct >= 80 ? '#22C55E' : pct >= 50 ? '#F59E0B' : '#EF4444'; }
  addChild(): void { this.router.navigate(['/auth/create-student'], { queryParams: { returnTo: 'parent' } }); }

  private readonly state = inject(AppStateService);
  logout(): void {
    this.state.logout();
    this.router.navigate(['/auth/login']);
  }
}
