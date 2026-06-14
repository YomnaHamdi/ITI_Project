import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { LessonAssignmentDto } from '../../../models/story.models';

@Component({
  selector: 'app-assigned-lessons',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './assigned-lessons.html',
  styleUrl: './assigned-lessons.css'
})
export class AssignedLessonsComponent implements OnInit {
  private readonly svc    = inject(StoryService);
  private readonly state  = inject(AppStateService);
  private readonly router = inject(Router);

  assignments = signal<LessonAssignmentDto[]>([]);
  loading     = signal(true);
  error       = signal('');

  ngOnInit(): void {
    const user = this.state.currentUser();
    if (!user?.id) { this.error.set('يرجى تسجيل الدخول.'); this.loading.set(false); return; }
    this.svc.getAssignedLessons(user.id).subscribe({
      next:  a => { this.assignments.set(a); this.loading.set(false); },
      error: () => { this.error.set('فشل تحميل الدروس المعيّنة.'); this.loading.set(false); }
    });
  }

  open(lessonId: string): void { this.router.navigate(['/lessons', lessonId]); }

  assignedTo(a: LessonAssignmentDto): string {
    if (a.targetType === 'Group')   return a.targetGroupName   ?? 'مجموعة';
    if (a.targetType === 'Student') return a.targetStudentName ?? 'طالب';
    return '';
  }
}
