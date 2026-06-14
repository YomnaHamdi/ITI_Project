import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { StoryService } from '../../../services/story';

interface TeacherRow {
  id:         string;
  name:       string;
  email:      string;
  subject:    string;
  students:   number;
  avgScore:   number;
  joinedAt:   string;
}

@Component({
  selector: 'app-school-teachers',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './school-teachers.component.html',
})
export class SchoolTeachersComponent implements OnInit {
  private readonly svc = inject(StoryService);

  readonly isLoading  = signal(false);
  readonly teachers   = signal<TeacherRow[]>([]);
  readonly searchTerm = signal('');

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

  setSearch(v: string) { this.searchTerm.set(v); }
  scoreColor(s: number): string { return s >= 80 ? '#22C55E' : s >= 60 ? '#F59E0B' : '#EF4444'; }
}
