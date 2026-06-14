import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { StoryService } from '../../services/story';
import { AppStateService } from '../../services/app-state-service';
import { LessonSummary } from '../../models/story.models';

@Component({
  selector: 'app-books',
  standalone: true,
  imports: [CommonModule, NavbarComponent, RouterLink],
  templateUrl: './books.component.html',
  styleUrl: './books.component.css'
})
export class BooksComponent implements OnInit {
  private readonly router  = inject(Router);
  private readonly route   = inject(ActivatedRoute);
  private readonly service = inject(StoryService);
  private readonly state   = inject(AppStateService);

  readonly levelId   = signal(1);
  readonly isLoading = signal(false);
  readonly error     = signal<string | null>(null);
  readonly lessons   = signal<LessonSummary[]>([]);

  readonly levelNames = ['', 'الحروف والأصوات', 'الكلمات والمفردات', 'الجمل والقصص'];

  readonly cardColors = ['#FFE4E8','#EDE9FE','#D1FAE5','#FFF7ED','#F0F9FF','#FFF0F3','#F0FFF4','#FFFBEB'];

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id')) || 1;
    this.levelId.set(id);
    this.load(id);
  }

  private load(level: number): void {
    this.isLoading.set(true);
    this.service.getLessonsByLevel(level).subscribe({
      next:  ls => { this.lessons.set(ls); this.isLoading.set(false); },
      error: ()  => { this.error.set('تعذّر تحميل الكتب.'); this.isLoading.set(false); }
    });
  }

  openBook(lesson: LessonSummary): void {
    this.router.navigate(['/lessons', lesson.id]);
  }

  // Progress state per lesson — checked against app state
  btnLabel(lesson: LessonSummary): string {
    const progress = this.state.lessonProgress(lesson.id);
    if (!progress) return 'ابدأ القراءة';
    if (progress.completed) return 'أعد القراءة';
    return 'واصل';
  }

  progressPercent(lesson: LessonSummary): number {
    const p = this.state.lessonProgress(lesson.id);
    if (!p || !p.totalPages) return 0;
    return Math.round(p.currentPage / p.totalPages * 100);
  }

  colorFor(i: number): string {
    return this.cardColors[i % this.cardColors.length];
  }
}
