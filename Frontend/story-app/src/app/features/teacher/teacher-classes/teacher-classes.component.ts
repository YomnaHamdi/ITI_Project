import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TeacherSidebarComponent } from '../teacher-shell/teacher-sidebar.component';
import { AuthService, StudentSummary } from '../../../services/auth.service';
import { AppStateService } from '../../../services/app-state-service';

interface ClassGroup {
  level:    number;
  label:    string;
  color:    string;
  students: StudentSummary[];
}

@Component({
  selector: 'app-teacher-classes',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TeacherSidebarComponent],
  templateUrl: './teacher-classes.component.html',
})
export class TeacherClassesComponent implements OnInit {
  private readonly auth   = inject(AuthService);
  private readonly state  = inject(AppStateService);
  private readonly router = inject(Router);

  readonly isLoading   = signal(false);
  readonly allStudents = signal<StudentSummary[]>([]);
  readonly searchTerm  = signal('');
  readonly activeLevel = signal<number | null>(null);

  readonly schoolCode = computed(() => this.state.currentUser()?.schoolCode ?? '');

  readonly classGroups = computed<ClassGroup[]>(() => {
    const students = this.allStudents();
    const term     = this.searchTerm().toLowerCase();
    const filtered = term
      ? students.filter(s => s.name.toLowerCase().includes(term) || s.username.toLowerCase().includes(term))
      : students;

    return [1, 2, 3].map(lv => ({
      level:    lv,
      label:    lv === 1 ? 'المستوى الأول' : lv === 2 ? 'المستوى الثاني' : 'المستوى الثالث',
      color:    lv === 1 ? '#F4788A' : lv === 2 ? '#8B5CF6' : '#22C55E',
      students: filtered.filter(s => s.level === lv),
    })).filter(g => this.activeLevel() === null ? true : g.level === this.activeLevel());
  });

  readonly totalStudents = computed(() => this.allStudents().length);

  ngOnInit(): void {
    this.isLoading.set(true);
    this.auth.getMyStudents().subscribe({
      next:  s => { this.allStudents.set(s); this.isLoading.set(false); },
      error: () => this.isLoading.set(false),
    });
  }

  setLevel(lv: number | null): void { this.activeLevel.set(lv); }

  addStudentToClass(): void {
    this.router.navigate(['/auth/create-student']);
  }

  goToAssignLesson(): void {
    this.router.navigate(['/teacher/lessons']);
  }

  levelColor(lv: number): string {
    return lv === 1 ? '#F4788A' : lv === 2 ? '#8B5CF6' : '#22C55E';
  }
}
