import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TeacherSidebarComponent } from '../teacher-shell/teacher-sidebar.component';
import { StoryService } from '../../../services/story';
import { LessonSummary } from '../../../models/story.models';

@Component({
  selector: 'app-teacher-lessons',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TeacherSidebarComponent],
  templateUrl: './teacher-lessons.component.html',
})
export class TeacherLessonsComponent implements OnInit {
  private readonly svc = inject(StoryService);

  readonly isLoading   = signal(false);
  readonly allLessons  = signal<LessonSummary[]>([]);
  readonly activeLevel = signal<number | null>(null);

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
  }

  setLevel(lv: number | null): void { this.activeLevel.set(lv); }

  levelLabel(lv: number): string { return `المستوى ${lv}`; }
}
