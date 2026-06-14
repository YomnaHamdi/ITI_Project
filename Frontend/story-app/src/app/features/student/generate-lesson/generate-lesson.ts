import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { StoryService } from '../../../services/story';
import { AppStateService } from '../../../services/app-state-service';

@Component({
  selector: 'app-generate-lesson',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './generate-lesson.html',
  styleUrl: './generate-lesson.css'
})
export class GenerateLessonComponent {
  private readonly svc    = inject(StoryService);
  private readonly state  = inject(AppStateService);
  private readonly router = inject(Router);

  topic   = '';
  letter  = '';
  level   = 1;

  loading = signal(false);
  error   = signal('');
  success = signal('');

  readonly levels = [
    { value: 1, label: 'المستوى 1 — الحروف (كلمتان)' },
    { value: 2, label: 'المستوى 2 — الكلمات (3 كلمات)' },
    { value: 3, label: 'المستوى 3 — الجمل (5 كلمات)' },
  ];

  readonly examples = [
    'القطة والكلب', 'البيت والحديقة', 'يوم في المدرسة',
    'الشمس والمطر', 'الأسد والنمر', 'الطعام اللذيذ'
  ];

  generate(): void {
    if (!this.topic.trim()) {
      this.error.set('يرجى كتابة موضوع الدرس.');
      return;
    }
    const user = this.state.currentUser();
    this.loading.set(true);
    this.error.set('');
    this.success.set('');

    this.svc.generateLesson({
      topic:       this.topic.trim(),
      letter:      this.letter.trim() || undefined,
      level:       this.level,
      creatorId:   user?.id,
      creatorRole: 'Student',
    }).subscribe({
      next: lesson => {
        this.loading.set(false);
        this.success.set('تم إنشاء الدرس! جاري الانتقال...');
        setTimeout(() => this.router.navigate(['/lessons', (lesson as any).id]), 1200);
      },
      error: (err: any) => {
        const detail = err?.error?.detail ?? err?.error?.error ?? '';
        this.error.set('فشل إنشاء الدرس.' + (detail ? ' ' + detail : ''));
        this.loading.set(false);
      }
    });
  }

  useExample(ex: string): void { this.topic = ex; }
}
