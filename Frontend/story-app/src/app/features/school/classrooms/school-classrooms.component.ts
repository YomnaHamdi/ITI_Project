import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { AuthService } from '../../../services/auth.service';

interface Classroom {
  id:           string;
  name:         string;
  level:        number;
  teacherId:    string;
  teacherName:  string;
  studentCount: number;
  avgProgress:  number;
  students?:    { id: string; name: string; username: string; level: number; placementDone: boolean }[];
  expanded:     boolean;
  loadingStudents: boolean;
}

@Component({
  selector: 'app-school-classrooms',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './school-classrooms.component.html',
})
export class SchoolClassroomsComponent implements OnInit {
  private readonly svc   = inject(StoryService);
  private readonly auth  = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly isLoading      = signal(false);
  readonly saving         = signal(false);
  readonly classrooms     = signal<Classroom[]>([]);
  readonly showForm       = signal(false);
  readonly schoolTeachers = signal<{ id: string; name: string }[]>([]);

  // Create form
  form = { name: '', teacherId: '', level: 1 };
  formError = '';

  // Edit mode
  editId     = signal<string | null>(null);
  editForm   = { name: '', teacherId: '', level: 1 };
  editError  = '';

  // Add-student panel per classroom
  addStudentFor  = signal<string | null>(null);
  studentSearch  = signal('');
  searchResults  = signal<{ id: string; name: string; username: string; level: number }[]>([]);
  searching      = signal(false);
  addingStudent  = signal(false);

  readonly filteredClassrooms = computed(() => this.classrooms());

  ngOnInit(): void {
    if (this.route.snapshot.queryParamMap.get('openForm') === '1') this.showForm.set(true);
    this.loadClassrooms();
    this.auth.getSchoolTeachers().subscribe({
      next: list => this.schoolTeachers.set(list),
      error: () => {}
    });
  }

  loadClassrooms(): void {
    this.isLoading.set(true);
    this.svc.getSchoolClassrooms().subscribe({
      next: list => {
        this.classrooms.set(list.map((c: any) => ({ ...c, expanded: false, loadingStudents: false })));
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  toggleForm(): void {
    this.showForm.update(v => !v);
    this.form = { name: '', teacherId: '', level: 1 };
    this.formError = '';
  }

  createClassroom(): void {
    if (!this.form.name.trim() || !this.form.teacherId) {
      this.formError = 'يرجى تعبئة اسم الفصل واختيار المعلم.';
      return;
    }
    this.saving.set(true);
    this.svc.createClassroom({ name: this.form.name.trim(), level: this.form.level, teacherId: this.form.teacherId })
      .subscribe({
        next: c => {
          this.classrooms.update(list => [{ ...c, expanded: false, loadingStudents: false }, ...list]);
          this.saving.set(false);
          this.toggleForm();
        },
        error: () => { this.formError = 'فشل الإنشاء. حاول مرة أخرى.'; this.saving.set(false); }
      });
  }

  startEdit(cls: Classroom): void {
    this.editId.set(cls.id);
    this.editForm = { name: cls.name, teacherId: cls.teacherId, level: cls.level };
    this.editError = '';
  }

  cancelEdit(): void { this.editId.set(null); }

  saveEdit(cls: Classroom): void {
    if (!this.editForm.name.trim()) { this.editError = 'الاسم مطلوب.'; return; }
    this.saving.set(true);
    this.svc.editClassroom(cls.id, { name: this.editForm.name, level: this.editForm.level, teacherId: this.editForm.teacherId })
      .subscribe({
        next: updated => {
          this.classrooms.update(list => list.map(c => c.id === cls.id
            ? { ...c, name: updated.name, level: updated.level, teacherId: updated.teacherId, teacherName: updated.teacherName }
            : c));
          this.editId.set(null);
          this.saving.set(false);
        },
        error: () => { this.editError = 'فشل التعديل.'; this.saving.set(false); }
      });
  }

  deleteClassroom(id: string): void {
    if (!confirm('هل أنت متأكد من حذف هذا الفصل؟')) return;
    this.svc.deleteClassroom(id).subscribe({
      next: () => this.classrooms.update(list => list.filter(c => c.id !== id)),
      error: () => alert('فشل الحذف.')
    });
  }

  toggleExpand(cls: Classroom): void {
    const expanded = !cls.expanded;
    this.classrooms.update(list => list.map(c => c.id === cls.id ? { ...c, expanded } : c));
    if (expanded && !cls.students) this.loadStudents(cls.id);
  }

  loadStudents(classroomId: string): void {
    this.classrooms.update(list => list.map(c => c.id === classroomId ? { ...c, loadingStudents: true } : c));
    this.svc.getClassroomDetail(classroomId).subscribe({
      next: detail => {
        this.classrooms.update(list => list.map(c => c.id === classroomId
          ? { ...c, students: detail.students, studentCount: detail.students.length, loadingStudents: false }
          : c));
      },
      error: () => this.classrooms.update(list => list.map(c => c.id === classroomId ? { ...c, loadingStudents: false } : c))
    });
  }

  removeStudent(classroomId: string, studentId: string): void {
    if (!confirm('حذف الطالب من الفصل؟')) return;
    this.svc.removeStudentFromClassroom(classroomId, studentId).subscribe({
      next: () => {
        this.classrooms.update(list => list.map(c => c.id === classroomId
          ? { ...c, students: c.students?.filter(s => s.id !== studentId), studentCount: (c.studentCount || 1) - 1 }
          : c));
      },
      error: () => alert('فشل الحذف.')
    });
  }

  openAddStudent(classroomId: string): void {
    this.addStudentFor.set(classroomId);
    this.studentSearch.set('');
    this.searchResults.set([]);
  }

  closeAddStudent(): void { this.addStudentFor.set(null); }

  onSearchInput(q: string): void {
    this.studentSearch.set(q);
    if (q.trim().length < 2) { this.searchResults.set([]); return; }
    this.searching.set(true);
    this.svc.searchSchoolStudents(q).subscribe({
      next: res => { this.searchResults.set(res); this.searching.set(false); },
      error: () => this.searching.set(false)
    });
  }

  addStudentToClass(classroomId: string, student: any): void {
    this.addingStudent.set(true);
    this.svc.addStudentToClassroom(classroomId, student.id).subscribe({
      next: () => {
        this.classrooms.update(list => list.map(c => {
          if (c.id !== classroomId) return c;
          const already = c.students?.some(s => s.id === student.id);
          return {
            ...c,
            studentCount: already ? c.studentCount : c.studentCount + 1,
            students: already ? c.students : [...(c.students ?? []), student]
          };
        }));
        this.addingStudent.set(false);
        this.closeAddStudent();
      },
      error: () => { alert('فشل الإضافة.'); this.addingStudent.set(false); }
    });
  }

  progressColor(p: number): string { return p >= 80 ? '#22C55E' : p >= 50 ? '#F59E0B' : '#EF4444'; }
  levelColor(l: number): string    { return ['','#F4788A','#8B5CF6','#22C55E'][l] ?? '#F4788A'; }
  levelLabel(l: number): string    { return ['','المستوى الأول','المستوى الثاني','المستوى الثالث'][l] ?? ''; }
}
