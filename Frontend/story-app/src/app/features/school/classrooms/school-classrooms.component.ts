import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';

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
  private readonly svc = inject(StoryService);

  readonly isLoading  = signal(false);
  readonly classrooms = signal<Classroom[]>([]);
  readonly showForm   = signal(false);

  form = { name: '', teacher: '', level: 1 };
  formError = '';

  ngOnInit(): void {
    this.isLoading.set(true);
    this.svc.getSchoolDashboard().subscribe({
      next: d => {
        this.classrooms.set([
          { id: '1', name: 'KG1 A', teacher: 'أ. فاطمة الزهراء', studentCount: 18, avgProgress: 75, level: 1 },
          { id: '2', name: 'KG1 B', teacher: 'أ. سارة العمري',   studentCount: 20, avgProgress: 60, level: 1 },
          { id: '3', name: 'KG2 A', teacher: 'أ. منى الشريف',    studentCount: 16, avgProgress: 88, level: 2 },
          { id: '4', name: 'KG2 B', teacher: 'أ. ليلى حسن',      studentCount: 19, avgProgress: 45, level: 2 },
          { id: '5', name: 'KG3 A', teacher: 'أ. فاطمة الزهراء', studentCount: 14, avgProgress: 92, level: 3 },
        ]);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  toggleForm(): void {
    this.showForm.update(v => !v);
    this.form = { name: '', teacher: '', level: 1 };
    this.formError = '';
  }

  createClassroom(): void {
    if (!this.form.name.trim() || !this.form.teacher.trim()) {
      this.formError = 'يرجى تعبئة اسم الفصل والمعلم.';
      return;
    }
    const newClassroom: Classroom = {
      id:           Date.now().toString(),
      name:         this.form.name.trim(),
      teacher:      this.form.teacher.trim(),
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
