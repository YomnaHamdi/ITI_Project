import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { TeacherSidebarComponent } from '../teacher-shell/teacher-sidebar.component';
import { StoryService } from '../../../services/story';
import { StudentDashboardDto } from '../../../models/story.models';

@Component({
  selector: 'app-student-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, TeacherSidebarComponent],
  templateUrl: './student-detail.component.html',
})
export class StudentDetailComponent implements OnInit {
  private readonly svc   = inject(StoryService);
  private readonly route = inject(ActivatedRoute);

  readonly isLoading  = signal(false);
  readonly studentName = signal('');
  readonly data        = signal<StudentDashboardDto | null>(null);
  readonly error       = signal<string | null>(null);

  ngOnInit(): void {
    const name = this.route.snapshot.paramMap.get('name') ?? '';
    this.studentName.set(name);
    if (!name) return;
    this.isLoading.set(true);
    this.svc.getStudentDashboard(name).subscribe({
      next:  d => { this.data.set(d); this.isLoading.set(false); },
      error: () => { this.isLoading.set(false); this.error.set('لم يتم العثور على بيانات هذا الطالب.'); }
    });
  }

  scoreColor(s: number): string { return s >= 80 ? '#22C55E' : s >= 50 ? '#F59E0B' : '#EF4444'; }
  formatDate(d: string): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('ar-SA', { year: 'numeric', month: 'short', day: 'numeric' });
  }
}
