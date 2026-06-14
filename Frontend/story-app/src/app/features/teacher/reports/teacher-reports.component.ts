import { Component, signal, inject, OnInit, computed } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { StoryService } from '../../../services/story';
import { TeacherSidebarComponent } from '../teacher-shell/teacher-sidebar.component';

@Component({
  selector: 'app-teacher-reports',
  standalone: true,
  imports: [CommonModule, DecimalPipe, TeacherSidebarComponent],
  templateUrl: './teacher-reports.component.html',
  styleUrl: './teacher-reports.component.css'
})
export class TeacherReportsComponent implements OnInit {
  private readonly service = inject(StoryService);

  readonly isLoading = signal(false);
  readonly data      = signal<any>(null);

  readonly maxScore = computed(() => {
    const students = this.data()?.students ?? [];
    return Math.max(...students.map((s: any) => s.averageExamScore), 1);
  });

  ngOnInit(): void {
    this.isLoading.set(true);
    this.service.getTeacherDashboard().subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  barHeight(score: number): number { return Math.round(score / this.maxScore() * 100); }

  barColor(score: number): string {
    return score >= 80 ? '#22C55E' : score >= 50 ? '#F59E0B' : '#EF4444';
  }
}
