import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';
import { StudentGroupDto, LessonSummary, AssignLessonRequest } from '../../../models/story.models';

@Component({
  selector: 'app-teacher-groups',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './teacher-groups.html',
  styleUrl: './teacher-groups.css'
})
export class TeacherGroupsComponent implements OnInit {
  private readonly svc   = inject(StoryService);
  private readonly state = inject(AppStateService);

  teacherId = signal('');
  groups    = signal<StudentGroupDto[]>([]);
  loading   = signal(false);
  error     = signal('');
  message   = signal('');

  // Create group
  newGroupName = '';

  // Add member
  addStudentId: { [groupId: string]: string } = {};

  // Assign lesson
  lessons  = signal<LessonSummary[]>([]);
  assignForm: { [groupId: string]: { lessonId: string; type: 'Student' | 'Group'; studentId: string } } = {};

  ngOnInit(): void {
    const user = this.state.currentUser();
    if (user?.id) {
      this.teacherId.set(user.id);
      this.loadGroups();
      this.loadLessons();
    }
  }

  loadGroups(): void {
    this.loading.set(true);
    this.svc.getTeacherGroups(this.teacherId()).subscribe({
      next:  g => { this.groups.set(g); this.loading.set(false); },
      error: () => { this.error.set('فشل تحميل المجموعات.'); this.loading.set(false); }
    });
  }

  loadLessons(): void {
    this.svc.getMyLessons(this.teacherId()).subscribe({
      next: l => this.lessons.set(l)
    });
  }

  createGroup(): void {
    if (!this.newGroupName.trim()) return;
    this.svc.createGroup(this.teacherId(), this.newGroupName.trim()).subscribe({
      next: g => {
        this.groups.update(list => [...list, g]);
        this.newGroupName = '';
        this.showMessage('تم إنشاء المجموعة.');
      },
      error: () => this.error.set('فشل إنشاء المجموعة.')
    });
  }

  addMember(groupId: string): void {
    const studentId = this.addStudentId[groupId]?.trim();
    if (!studentId) return;
    this.svc.addGroupMember(groupId, studentId).subscribe({
      next: () => { this.addStudentId[groupId] = ''; this.loadGroups(); this.showMessage('تمت إضافة الطالب.'); },
      error: () => this.error.set('فشل إضافة الطالب.')
    });
  }

  removeMember(groupId: string, studentId: string): void {
    this.svc.removeGroupMember(groupId, studentId).subscribe({
      next: () => { this.loadGroups(); this.showMessage('تمت إزالة الطالب.'); },
      error: () => this.error.set('فشل إزالة الطالب.')
    });
  }

  deleteGroup(groupId: string): void {
    if (!confirm('هل تريد حذف هذه المجموعة؟')) return;
    this.svc.deleteGroup(groupId).subscribe({
      next: () => { this.groups.update(list => list.filter(g => g.id !== groupId)); this.showMessage('تم حذف المجموعة.'); },
      error: () => this.error.set('فشل حذف المجموعة.')
    });
  }

  assignLesson(groupId: string): void {
    const form = this.assignForm[groupId];
    if (!form?.lessonId) return;
    const req: AssignLessonRequest = {
      lessonId:       form.lessonId,
      targetType:     form.type,
      targetGroupId:  form.type === 'Group' ? groupId : undefined,
      targetStudentId: form.type === 'Student' ? form.studentId : undefined
    };
    this.svc.assignLesson(req).subscribe({
      next: () => this.showMessage('تم تعيين الدرس بنجاح.'),
      error: () => this.error.set('فشل تعيين الدرس.')
    });
  }

  initAssignForm(groupId: string): void {
    if (!this.assignForm[groupId])
      this.assignForm[groupId] = { lessonId: '', type: 'Group', studentId: '' };
  }

  private showMessage(msg: string): void {
    this.message.set(msg);
    setTimeout(() => this.message.set(''), 3000);
  }
}
