import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule, DecimalPipe } from '@angular/common';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';

@Component({
  selector: 'app-school-dashboard',
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink, NavbarComponent],
  templateUrl: './school-dashboard.component.html',
  styleUrl: './school-dashboard.component.css'
})
export class SchoolDashboardComponent implements OnInit {
  private readonly service = inject(StoryService);
  private readonly router  = inject(Router);

  readonly isLoading = signal(false);
  readonly data      = signal<any>(null);

  readonly classrooms        = computed(() => (this.data()?.classrooms         ?? []) as any[]);
  readonly levelDistribution = computed(() => (this.data()?.levelDistribution  ?? []) as any[]);
  readonly maxClassProgress  = computed(() => Math.max(...this.classrooms().map((c: any) => c.avgProgress), 1));

  ngOnInit(): void {
    this.isLoading.set(true);
    this.service.getSchoolDashboard().subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  progressColor(pct: number): string { return pct >= 80 ? '#22C55E' : pct >= 50 ? '#F59E0B' : '#EF4444'; }
  barWidth(pct: number): number { return Math.round(pct / this.maxClassProgress() * 100); }
  private readonly state = inject(AppStateService);

  goToTeacher(): void { this.router.navigate(['/teacher/students']); }
  addStudent():  void { this.router.navigate(['/auth/create-student']); }
  logout(): void {
    this.state.logout();
    this.router.navigate(['/auth/login']);
  }
}
