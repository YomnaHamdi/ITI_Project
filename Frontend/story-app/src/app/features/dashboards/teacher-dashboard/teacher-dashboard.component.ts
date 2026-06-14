import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { StoryService } from '../../../services/story';
import { TeacherSidebarComponent } from '../../teacher/teacher-shell/teacher-sidebar.component';

@Component({
  selector: 'app-teacher-dashboard',
  standalone: true,
  imports: [CommonModule, DecimalPipe, FormsModule, RouterLink, TeacherSidebarComponent],
  templateUrl: './teacher-dashboard.component.html',
  styleUrl: './teacher-dashboard.component.css'
})
export class TeacherDashboardComponent implements OnInit {
  private readonly service = inject(StoryService);
  private readonly router  = inject(Router);

  readonly isLoading  = signal(false);
  readonly data       = signal<any>(null);
  readonly searchTerm = signal('');

  readonly filteredStudents = computed(() => {
    const d = this.data();
    const t = this.searchTerm().toLowerCase().trim();
    if (!d?.students) return [];
    return t
      ? d.students.filter((s: any) => s.childName.toLowerCase().includes(t))
      : d.students;
  });

  ngOnInit(): void {
    this.isLoading.set(true);
    this.service.getTeacherDashboard().subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => this.isLoading.set(false)
    });
  }

  levelColor(level: string): string {
    return level === 'ممتاز' ? '#16A34A' : level === 'جيد' ? '#D97706' : '#DC2626';
  }

  progressColor(pct: number): string {
    return pct >= 80 ? '#22C55E' : pct >= 50 ? '#F59E0B' : '#EF4444';
  }

  lastActiveLabel(dateStr: string | null): string {
    if (!dateStr) return '—';
    const diff = Math.floor((Date.now() - new Date(dateStr).getTime()) / 86400000);
    if (diff === 0) return 'اليوم';
    if (diff === 1) return 'أمس';
    return `منذ ${diff} أيام`;
  }

  addStudent(): void {
    this.router.navigate(['/auth/create-student'], { queryParams: { returnTo: 'teacher' } });
  }

  viewStudent(name: string): void {
    this.router.navigate(['/teacher/students', name]);
  }
}
