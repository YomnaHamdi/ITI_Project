import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';
import { AuthService } from '../../../services/auth.service';

interface Classroom {
  id:           string;
  name:         string;
  teacher:      string;
  studentCount: number;
  avgProgress:  number;
  level:        number;
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

  readonly isLoading        = signal(false);
  readonly classrooms       = signal<Classroom[]>([]);
  readonly showForm         = signal(false);
  readonly schoolTeachers   = signal<{ id: string; name: string }[]>([]);

  form = { name: '', teacherId: '', level: 1 };
  formError = '';

  ngOnInit(): void {
    if (this.route.snapshot.queryParamMap.get('openForm') === '1') {
      this.showForm.set(true);
    }
    this.isLoading.set(true);
    this.svc.getSchoolDashboard().subscribe({
      next: d => {
        this.classrooms.set((d.classrooms ?? []).map((c: any, i: number) => ({
          id:           String(i + 1),
          name:         c.name,
          teacher:      c.teacher,
          studentCount: c.students,
          avgProgress:  c.avgProgress,
          level:        1,
        })));
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
    this.auth.getSchoolTeachers().subscribe({
      next: list => this.schoolTeachers.set(list),
      error: () => {}
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
    const teacher = this.schoolTeachers().find(t => t.id === this.form.teacherId);
    const newClassroom: Classroom = {
      id:           Date.now().toString(),
      name:         this.form.name.trim(),
      teacher:      teacher?.name ?? '',
      studentCount: 0,
      avgProgress:  0,
      level:        this.form.level,
    };
    this.classrooms.update(list => [newClassroom, ...list]);
    this.toggleForm();
  }

  progressColor(p: number): string { return p >= 80 ? '#22C55E' : p >= 50 ? '#F59E0B' : '#EF4444'; }
  levelColor(l: number): string { return ['','#F4788A','#8B5CF6','#22C55E'][l] ?? '#F4788A'; }
}
