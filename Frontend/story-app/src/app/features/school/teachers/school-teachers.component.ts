import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { AuthService } from '../../../services/auth.service';
import { AppStateService } from '../../../services/app-state-service';

interface TeacherRow {
  id:       string;
  name:     string;
  email:    string;
  subject:  string;
  students: number;
  avgScore: number;
  joinedAt: string;
}

@Component({
  selector: 'app-school-teachers',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './school-teachers.component.html',
})
export class SchoolTeachersComponent implements OnInit {
  private readonly svc   = inject(StoryService);
  private readonly auth  = inject(AuthService);
  private readonly state = inject(AppStateService);

  readonly isLoading  = signal(false);
  readonly isSaving   = signal(false);
  readonly teachers   = signal<TeacherRow[]>([]);
  readonly searchTerm = signal('');
  readonly showForm   = signal(false);

  form = { name: '', email: '', password: '' };
  formError  = '';
  formSuccess = '';

  readonly filtered = computed(() => {
    const q = this.searchTerm().toLowerCase();
    return !q ? this.teachers() : this.teachers().filter(t =>
      t.name.toLowerCase().includes(q) || t.email.toLowerCase().includes(q)
    );
  });

  ngOnInit(): void {
    this.isLoading.set(true);
    this.svc.getSchoolDashboard().subscribe({
      next: d => {
        const mock: TeacherRow[] = [
          { id: '1', name: 'أ. فاطمة الزهراء', email: 'fatima@school.edu', subject: 'اللغة العربية', students: 18, avgScore: 75, joinedAt: '2024-09-01' },
          { id: '2', name: 'أ. سارة العمري',    email: 'sara@school.edu',   subject: 'اللغة العربية', students: 20, avgScore: 82, joinedAt: '2024-09-01' },
          { id: '3', name: 'أ. منى الشريف',     email: 'mona@school.edu',   subject: 'اللغة العربية', students: 16, avgScore: 90, joinedAt: '2024-09-15' },
          { id: '4', name: 'أ. ليلى حسن',       email: 'laila@school.edu',  subject: 'اللغة العربية', students: 19, avgScore: 68, joinedAt: '2024-10-01' },
        ].slice(0, d.totalTeachers || 4);
        this.teachers.set(mock);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  toggleForm(): void {
    this.showForm.update(v => !v);
    this.form = { name: '', email: '', password: '' };
    this.formError  = '';
    this.formSuccess = '';
  }

  createTeacher(): void {
    this.formError  = '';
    this.formSuccess = '';
    if (!this.form.name.trim() || !this.form.email.trim() || !this.form.password.trim()) {
      this.formError = 'يرجى تعبئة جميع الحقول.';
      return;
    }
    if (this.form.password.length < 6) {
      this.formError = 'كلمة المرور يجب أن تكون 6 أحرف على الأقل.';
      return;
    }

    const userId = this.state.currentUser()?.id ?? '';
    const schoolCode = userId.replace(/-/g, '').substring(0, 8).toUpperCase();

    this.isSaving.set(true);
    this.auth.register({
      fullName:   this.form.name.trim(),
      email:      this.form.email.trim(),
      password:   this.form.password,
      role:       'teacher',
      schoolCode,
    }).subscribe({
      next: res => {
        const newTeacher: TeacherRow = {
          id:       res.userId,
          name:     res.name,
          email:    this.form.email.trim(),
          subject:  'اللغة العربية',
          students: 0,
          avgScore: 0,
          joinedAt: new Date().toISOString().slice(0, 10),
        };
        this.teachers.update(list => [newTeacher, ...list]);
        this.formSuccess = `تم إنشاء حساب المعلم ${res.name} بنجاح.`;
        this.form = { name: '', email: '', password: '' };
        this.isSaving.set(false);
      },
      error: (err: any) => {
        this.formError = err?.error?.error ?? 'تعذّر إنشاء الحساب. تحقق من البيانات.';
        this.isSaving.set(false);
      }
    });
  }

  setSearch(v: string) { this.searchTerm.set(v); }
  scoreColor(s: number): string { return s >= 80 ? '#22C55E' : s >= 60 ? '#F59E0B' : '#EF4444'; }
}
