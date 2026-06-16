import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TeacherSidebarComponent } from '../teacher-shell/teacher-sidebar.component';
import { StoryService } from '../../../services/story';

interface ClassStudent {
  id:            string;
  name:          string;
  username:      string;
  level:         number;
  placementDone: boolean;
}

interface ClassroomGroup {
  classroomId:   string;
  classroomName: string;
  level:         number;
  label:         string;
  color:         string;
  students:      ClassStudent[];
}

@Component({
  selector: 'app-teacher-classes',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TeacherSidebarComponent],
  templateUrl: './teacher-classes.component.html',
})
export class TeacherClassesComponent implements OnInit {
  private readonly svc    = inject(StoryService);
  private readonly router = inject(Router);

  readonly isLoading   = signal(false);
  readonly classrooms  = signal<any[]>([]);
  readonly searchTerm  = signal('');
  readonly activeLevel = signal<number | null>(null);

  readonly allStudents = computed<ClassStudent[]>(() => {
    const seen = new Set<string>();
    const out:  ClassStudent[] = [];
    for (const c of this.classrooms()) {
      for (const s of (c.students ?? [])) {
        if (!seen.has(s.id)) { seen.add(s.id); out.push(s); }
      }
    }
    return out;
  });

  readonly classGroups = computed<ClassroomGroup[]>(() => {
    const term = this.searchTerm().toLowerCase();
    const lv   = this.activeLevel();
    return this.classrooms()
      .filter(c => lv === null || c.level === lv)
      .map(c => ({
        classroomId:   c.id,
        classroomName: c.name,
        level:         c.level,
        label:         c.name,
        color:         this.levelColor(c.level),
        students:      (c.students ?? []).filter((s: ClassStudent) =>
          !term || s.name.toLowerCase().includes(term) || s.username.toLowerCase().includes(term)
        ),
      }));
  });

  readonly totalStudents = computed(() => this.allStudents().length);

  ngOnInit(): void {
    this.isLoading.set(true);
    this.svc.getMyTeacherClassrooms().subscribe({
      next:  list => { this.classrooms.set(list); this.isLoading.set(false); },
      error: ()   => this.isLoading.set(false),
    });
  }

  setLevel(lv: number | null): void { this.activeLevel.set(lv); }

  addStudentToClass(): void { this.router.navigate(['/auth/create-student']); }

  goToAssignLesson(classroomLevel?: number): void {
    const extras = classroomLevel ? { queryParams: { level: classroomLevel } } : {};
    this.router.navigate(['/teacher/lessons'], extras);
  }

  levelColor(lv: number): string {
    return lv === 1 ? '#F4788A' : lv === 2 ? '#8B5CF6' : '#22C55E';
  }
}
