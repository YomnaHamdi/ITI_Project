import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TeacherSidebarComponent } from '../teacher-shell/teacher-sidebar.component';
import { StoryService } from '../../../services/story';
import { AuthService, StudentSummary } from '../../../services/auth.service';
import { AppStateService } from '../../../services/app-state-service';
import { LessonSummary, StudentGroupDto } from '../../../models/story.models';

type AssignMode = 'Student' | 'Level' | 'Group';

interface AssignPanel {
  lessonId:   string;
  mode:       AssignMode;
  studentId:  string;
  level:      number;
  groupId:    string;
  saving:     boolean;
  success:    string;
  error:      string;
}

@Component({
  selector: 'app-teacher-lessons',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TeacherSidebarComponent],
  templateUrl: './teacher-lessons.component.html',
})
export class TeacherLessonsComponent implements OnInit {
  private readonly svc   = inject(StoryService);
  private readonly auth  = inject(AuthService);
  private readonly state = inject(AppStateService);

  readonly isLoading   = signal(false);
  readonly allLessons  = signal<LessonSummary[]>([]);
  readonly activeLevel = signal<number | null>(null);
  readonly students    = signal<StudentSummary[]>([]);
  readonly groups      = signal<StudentGroupDto[]>([]);
  readonly assignPanel = signal<AssignPanel | null>(null);

  readonly filtered = computed(() => {
    const lv = this.activeLevel();
    return lv === null ? this.allLessons() : this.allLessons().filter(l => l.level === lv);
  });

  readonly levelCounts = computed(() => {
    const ls = this.allLessons();
    return [1, 2, 3].map(lv => ({ lv, count: ls.filter(l => l.level === lv).length }));
  });

  ngOnInit(): void {
    this.isLoading.set(true);
    let done = 0;
    const combined: LessonSummary[] = [];
    [1, 2, 3].forEach(lv => {
      this.svc.getLessonsByLevel(lv).subscribe({
        next:  ls => { combined.push(...ls); done++; if (done === 3) { this.allLessons.set(combined); this.isLoading.set(false); } },
        error: ()  => { done++; if (done === 3) { this.allLessons.set(combined); this.isLoading.set(false); } }
      });
    });

    // Pre-load students and groups for assignment
    this.auth.getMyStudents().subscribe({ next: s => this.students.set(s), error: () => {} });
    const userId = this.state.currentUser()?.id;
    if (userId) {
      this.svc.getTeacherGroups(userId).subscribe({ next: g => this.groups.set(g), error: () => {} });
    }
  }

  setLevel(lv: number | null): void { this.activeLevel.set(lv); }
  levelLabel(lv: number): string { return `المستوى ${lv}`; }

  openAssign(lessonId: string): void {
    const current = this.assignPanel();
    if (current?.lessonId === lessonId) { this.assignPanel.set(null); return; }
    this.assignPanel.set({ lessonId, mode: 'Student', studentId: '', level: 1, groupId: '', saving: false, success: '', error: '' });
  }

  setMode(mode: AssignMode): void {
    this.assignPanel.update(p => p ? { ...p, mode, success: '', error: '' } : null);
  }

  assign(): void {
    const panel = this.assignPanel();
    if (!panel) return;

    if (panel.mode === 'Student' && !panel.studentId) {
      this.assignPanel.update(p => p ? { ...p, error: 'يرجى اختيار طالب.' } : null);
      return;
    }
    if (panel.mode === 'Group' && !panel.groupId) {
      this.assignPanel.update(p => p ? { ...p, error: 'يرجى اختيار مجموعة.' } : null);
      return;
    }
    if (panel.mode === 'Level') {
      this.assignByLevel(panel);
      return;
    }

    this.assignPanel.update(p => p ? { ...p, saving: true, error: '' } : null);
    const req = panel.mode === 'Student'
      ? { lessonId: panel.lessonId, targetType: 'Student' as const, targetStudentId: panel.studentId }
      : { lessonId: panel.lessonId, targetType: 'Group'   as const, targetGroupId:   panel.groupId };

    this.svc.assignLesson(req).subscribe({
      next:  () => this.assignPanel.update(p => p ? { ...p, saving: false, success: 'تم التعيين بنجاح! ✅', error: '' } : null),
      error: (err: any) => this.assignPanel.update(p => p ? {
        ...p, saving: false,
        error: err?.error?.error ?? 'فشل التعيين. حاول مرة أخرى.'
      } : null)
    });
  }

  private assignByLevel(panel: AssignPanel): void {
    const targets = this.students().filter(s => s.level === panel.level);
    if (targets.length === 0) {
      this.assignPanel.update(p => p ? { ...p, error: `لا يوجد طلاب في المستوى ${panel.level}.` } : null);
      return;
    }
    this.assignPanel.update(p => p ? { ...p, saving: true, error: '' } : null);
    let done = 0; let failed = 0;
    targets.forEach(s => {
      this.svc.assignLesson({ lessonId: panel.lessonId, targetType: 'Student', targetStudentId: s.id }).subscribe({
        next:  () => { done++; if (done + failed === targets.length) this.finishLevelAssign(done, failed); },
        error: () => { failed++; if (done + failed === targets.length) this.finishLevelAssign(done, failed); }
      });
    });
  }

  private finishLevelAssign(done: number, failed: number): void {
    const msg = failed === 0
      ? `تم تعيين الدرس لـ ${done} طلاب ✅`
      : `تم تعيين لـ ${done} طلاب، فشل ${failed}`;
    this.assignPanel.update(p => p ? { ...p, saving: false, success: msg, error: failed > 0 ? 'بعض التعيينات فشلت.' : '' } : null);
  }

  deleteLesson(id: string): void {
    if (!confirm('هل تريد حذف هذا الدرس؟')) return;
    this.svc.deleteLesson(id).subscribe({
      next:  () => this.allLessons.update(ls => ls.filter(l => l.id !== id)),
      error: () => alert('فشل الحذف. حاول مرة أخرى.')
    });
  }
}
