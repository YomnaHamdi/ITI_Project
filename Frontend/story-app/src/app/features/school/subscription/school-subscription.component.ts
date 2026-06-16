import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';

@Component({
  selector: 'app-school-subscription',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './school-subscription.component.html',
})
export class SchoolSubscriptionComponent implements OnInit {
  private readonly svc = inject(StoryService);

  readonly isLoading   = signal(false);
  readonly dashboard   = signal<any>(null);
  readonly classrooms  = signal<any[]>([]);

  readonly totalStudents  = computed(() => this.dashboard()?.totalStudents  ?? 0);
  readonly totalTeachers  = computed(() => this.dashboard()?.totalTeachers  ?? 0);
  readonly totalClassrooms = computed(() => this.classrooms().length);

  readonly planName    = 'الخطة المجانية';
  readonly planLimit   = { students: 50, teachers: 5, classrooms: 10 };
  readonly renewDate   = 'غير محدد';
  readonly contactEmail = 'support@lughati.com';

  ngOnInit(): void {
    this.isLoading.set(true);
    this.svc.getSchoolDashboard().subscribe({
      next:  d => { this.dashboard.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
    this.svc.getSchoolClassrooms().subscribe({
      next:  list => this.classrooms.set(list),
      error: () => {}
    });
  }

  usagePct(used: number, limit: number): number {
    return Math.min(Math.round(used / limit * 100), 100);
  }

  usageColor(pct: number): string {
    return pct >= 90 ? '#EF4444' : pct >= 70 ? '#F59E0B' : '#22C55E';
  }
}
