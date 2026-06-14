import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StoryService } from '../../services/story';
import { AppStateService } from '../../services/app-state-service';
import { LoadingComponent } from '../../shared/loading/loading';
import { ErrorToastComponent } from '../../shared/error-toast/error-toast';
import { LessonSummary } from '../../models/story.models';

@Component({
  selector: 'app-lessons-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingComponent, ErrorToastComponent],
  templateUrl: './lessons-list.html',
  styleUrl: './lessons-list.css'
})
export class LessonsListComponent implements OnInit {
  private readonly storyService = inject(StoryService);
  private readonly state        = inject(AppStateService);
  private readonly router       = inject(Router);

  readonly isLoading = signal(false);
  readonly error     = signal<string | null>(null);
  readonly lessons   = signal<LessonSummary[]>([]);
  readonly level     = signal(1);

  readonly levels = [
    { value: 1, label: 'المستوى 1' },
    { value: 2, label: 'المستوى 2' },
    { value: 3, label: 'المستوى 3' },
    { value: 4, label: 'المستوى 4' }
  ];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.storyService.getLessonsByLevel(this.level()).subscribe({
      next: list => { this.lessons.set(list); this.isLoading.set(false); },
      error: () => { this.error.set('تعذّر تحميل الدروس.'); this.isLoading.set(false); }
    });
  }

  selectLevel(v: number): void { this.level.set(v); this.load(); }

  openLesson(lesson: LessonSummary): void {
    this.router.navigate(['/lessons', lesson.id]);
  }

  goHome():  void { this.router.navigate(['/']); }
  goAdmin(): void { this.router.navigate(['/admin/import']); }
}